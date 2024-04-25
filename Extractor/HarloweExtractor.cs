#if UNITY_EDITOR
using Vine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
public class HarloweExtractor : FormatExtractor
{
    List<string> _RawLines;
    List<string> _ProcessedLines;
    int _InsertCounter;
    public HarloweExtractor(string input) : base(input)
    {
        _RawLines = input.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        _ProcessedLines = new List<string>();
    }
    public void AddLine(string line)
    {
        _ProcessedLines.Add(line);
        //Debug.Log(line);
    }
    public override string[] Extract()
    {
        while(_RawLines.Count > 0)
        {
            string nextLineToProcess = DrawFetchLine();
            if (!string.IsNullOrWhiteSpace(nextLineToProcess))
            {
                string processedLine = FilterAndProcessLine(nextLineToProcess);
                AddLine(processedLine);
            }
        }
        return _ProcessedLines.ToArray();
    }
    void ExtractInternalBlock(List<string> lines)
    {
        foreach (string line in lines)
        {
            string processedLine = FilterAndProcessLine(line);
            AddLine(processedLine);
        }
    }
    public string DrawFetchLine()
    {
        if (_RawLines.Count <= 0)
            return string.Empty;
        string fetchLine = _RawLines[0];
        _RawLines.RemoveAt(0);
        return fetchLine;
    }
    //main condition thread, process all function macro and output lines
    string FilterAndProcessLine(string lineToProcess)
    {
        lineToProcess = lineToProcess.Trim();
        if (IsMacro(lineToProcess))
        {//Is Line a Macro?
            if (Regex.IsMatch(lineToProcess, @"^\((if|else|else-?if):.*\)", RegexOptions.IgnoreCase))
                return ProcessBranchingMacro(lineToProcess);
            else if (IsMarkedText(lineToProcess))
                return ProcessMarkedText(lineToProcess);
            else if (IsDelayedLink(lineToProcess))
                return ProcessDelayedLink(lineToProcess);
            else if (IsSetMacro(lineToProcess))
                return ProcessSetMacro(lineToProcess);
            else if (IsRestartMacro(lineToProcess))
                return ProcessRestartMacro(lineToProcess);
            else if (IsGotoMacro(lineToProcess))
                return ProcessGotoMacro(lineToProcess);
        }
        else if (IsClickLambda(lineToProcess))
            return ProcessClickLambda(lineToProcess);
        else if (lineToProcess.StartsWith("#"))//Check header
            return ProcessHeader(lineToProcess);
        else if (IsLinkHook(lineToProcess))
            return ProcessLinkHook(lineToProcess);
        else if (lineToProcess.Contains(":"))//assume it's a dialogue line since its not a macro and doesn't start with a #
        {
            lineToProcess = Regex.Replace(lineToProcess, @"//([^//]+)//", @"<i>$1</i>");
            lineToProcess = Regex.Replace(lineToProcess, @"\(text-colour: *(?<color>[A-Za-z]+) *\) *\[(?<text>[^\[\]]+)\]", @"<color=$1>$2</color>", RegexOptions.ExplicitCapture);
            lineToProcess = ProcessBranchingInString(lineToProcess);
            if (Regex.IsMatch(lineToProcess, @"^([^:\(\)]+)\(([A-Za-z]+)\): ?(.+)$"))
            {
                Match t = Regex.Match(lineToProcess, @"^(?<character>[^:\(\)]+)\((?<emotion>[A-Za-z]+)\): ?(?<speech>.+)$", RegexOptions.ExplicitCapture);
                GroupCollection m = t.Groups;
                string character = m[nameof(character)].Value;
                VineCharacterEmotion emotion = (VineCharacterEmotion)Enum.Parse(typeof(VineCharacterEmotion), m[nameof(emotion)].Value.ToLower());
                string speech = m[nameof(speech)].Value.Trim();
                speech = ProcessVariableInString(speech);
                return $"yield return new {nameof(VineLineOutput)}(\"{character}\", {nameof(VineCharacterEmotion)}.{emotion}, $\"{speech}\");" + "\n";
            }
            else if (Regex.IsMatch(lineToProcess, @"([^:]+): *(.+)"))
            {
                lineToProcess = Regex.Replace(lineToProcess, @"(?<character>[^:]+): *(?<speech>.+)", "$1\", $\"$2");
                return $"yield return new {nameof(VineLineOutput)}(\"{lineToProcess}\");" + "\n";
            }
        }


        //throw new Exception();
        AddLine("//TODO check this!! Does not match any known pattern");
        return ProcessValueStatement(lineToProcess) + ";\n";
    }
    #region Simple Process
    string ProcessMarkedText(string lineToProcess)
    {
        Match t = Regex.Match(lineToProcess, @"\[(?<messageWithArguments>.+)\]$", RegexOptions.ExplicitCapture);
        string messageWithArguments = t.Groups[nameof(messageWithArguments)].Value;
        //messageWithArguments = ProcessBranchingInString(messageWithArguments);
        //Branching should be outside, same as C#
        messageWithArguments = Regex.Replace(messageWithArguments, ", *", "\", \"");
        messageWithArguments = ProcessVariableInString(messageWithArguments);
        return $"yield return new {nameof(UniVineMarkedOutput)}(\"{messageWithArguments}\");" + "\n";
    }
    string ProcessHeader(string lineToProcess)
    {
        string header = lineToProcess.Remove(0, 1);//remove the # at the start
        string body = DrawFetchLine();
        return $"yield return new {nameof(VineHeaderOutput)}(\"{header}\", \"{body}\");" + "\n";
    }
    string ProcessLinkHook(string lineToProcess)
    {
        Match l = Regex.Match(lineToProcess, @"^\[\[(?<labelWithPassageName>.+)\]\]$", RegexOptions.ExplicitCapture);
        string labelWithPassageName = l.Groups[nameof(labelWithPassageName)].Value;
        labelWithPassageName = labelWithPassageName.Replace("|", "\",\"");
        return ($"yield return new {nameof(VineLinkOutput)}(\"{labelWithPassageName}\");") + "\n";
    }
    #endregion
    #region Complex Macro Process
    string ProcessBranchingMacro(string lineToProcess)
    {//is branching macro
        string keyword, conditionBool;
        List<string> codeBlock; 
        Match m = Regex.Match(lineToProcess, @"^\((?<keyword>if|else|else-?if):(?<conditionBool>[^\[\]]*)\) *(?<topLine>.*)$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
        GroupCollection g = m.Groups;

        keyword = g[nameof(keyword)].Value;
        conditionBool = g[nameof(conditionBool)].Value;
        conditionBool = ProcessValueStatement(conditionBool);
        string branchStatement = ProcessBranchingStatement(keyword, conditionBool);
        AddLine($"{branchStatement}\n" +"{");

        string topLine = g[nameof(topLine)].Value.Trim();
        codeBlock = MarkOutInternalBlock(topLine);
        ExtractInternalBlock(codeBlock);

        return "}\n";
    }
    bool IsClickLambda(string rawLine)
    {
        if (Regex.IsMatch(rawLine, "(?<front>.+)\\(click: *\"(?<textClick>.+)\" *\\)", RegexOptions.IgnoreCase))
        {
            Match m = Regex.Match(rawLine, "(?<front>.+)\\(click: *\"(?<textClick>.+)\" *\\)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            string front = m.Groups[nameof(front)].Value.Trim();
            string textClick = m.Groups[nameof(textClick)].Value.Trim();
            return front == textClick;
        }
        return false;
    }
    string ProcessClickLambda(string lineToProcess)
    {
        Match m = Regex.Match(lineToProcess, "^(?<front>.+)\\(click: *\"(?<textClick>.+)\" *\\) *(?<topLine>.*)$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
        GroupCollection g = m.Groups;
        string front = g[nameof(front)].Value.Trim();
        string textClick = g[nameof(textClick)].Value.Trim();
        //TODO WHATS WRONG???
        UnityEngine.Debug.Log(front);
        UnityEngine.Debug.Log(textClick);
        string topLine = g[nameof(topLine)].Value.Trim();
        List<string> lambdaBlock = MarkOutInternalBlock(topLine);
        //textClick = ProcessMacrosInString(textClick);
        if(TryProcessClickFunc(lambdaBlock, out string clickFunc))
            return $"yield return new {nameof(VineClickActionOutput)}(\"{front}\", {clickFunc});";
        else
        {
            AddLine($"yield return new {nameof(VineClickActionOutput)}(\"{front}\", () =>\n" + "{");
            ExtractInternalBlock(lambdaBlock);
            return "});\n";
        }
    }
    bool IsDelayedLink(string input)
    {
        return Regex.IsMatch(input, @"^\(event: *when +time *>=? *\d*\.?\d+s *\)", RegexOptions.IgnoreCase);
    }
    bool IsSetMacro(string input)
    {
        return Regex.IsMatch(input, @"^\(set: *\$(.+) to (.+)\)$", RegexOptions.IgnoreCase);
    }
    string ProcessSetMacro(string input)
    {
        Match m = Regex.Match(input,
            @"^\(set: *\$(?<variableToSet>.+) to (?<valueStatement>.+)\)$",
            RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
        GroupCollection groups = m.Groups;
        string variableToSet = groups[nameof(variableToSet)].Value;
        string valueStatement = groups[nameof(valueStatement)].Value;
        valueStatement = ProcessValueStatement(valueStatement);
        return $"Set[\"{variableToSet}\"] = {valueStatement};";
    }
    bool IsRestartMacro(string input)
    {
        return Regex.IsMatch(input, @"^\(restart: *\)$", RegexOptions.IgnoreCase);
    }
    string ProcessRestartMacro(string input)
    {
        return Regex.Replace(input, @"^\(restart: *\)$", "Restart()", RegexOptions.IgnoreCase);
    }
    bool IsGotoMacro(string input)
    {
        return Regex.IsMatch(input, @"^\(go-to: *(?<passageName>.+) *\)$", RegexOptions.IgnoreCase);
    }
    string ProcessGotoMacro(string input)
    {
        Match m = Regex.Match(input, @"^\(go-to: *(?<passageName>.+) *\)$",  RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
        string passageName = m.Groups[nameof(passageName)].Value;
        passageName = ProcessVariableInString(passageName);
        return $"GoTo($\"{passageName}\");";
    }
    string ProcessDelayedLink(string lineToProcess)
    {
        Match t = Regex.Match(lineToProcess, @"^\(event: *when +time *>=? *(?<time>\d*\.?\d+)s *\)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
        GroupCollection m = t.Groups;
        string time = m[nameof(time)].Value;
        List<string> lambdaBlock = MarkOutInternalBlock(DrawFetchLine());
        if (TryProcessClickFunc(lambdaBlock, out string clickFunc))
            return $"yield return new {nameof(VineDelayActionOutput)}({time}), {clickFunc})";
        else
        {
            AddLine($"yield return new {nameof(VineDelayActionOutput)}({time}), () =>\n" + "{");
            ExtractInternalBlock(lambdaBlock);
            return "});\n";
        }
    }
    string ProcessBranchingInString(string input)
    {
        if (Regex.IsMatch(input, @"\(\w+-?\w+:.*\)"))
        {
            if (Regex.IsMatch(input, @"\((if|else|else-?if):.+\) *\[.*\]", RegexOptions.IgnoreCase))//check inline branching
            {
                MatchCollection rawChains = Regex.Matches(input, @"\((if|else|else-?if):.+\) *\[.*\]", RegexOptions.IgnoreCase);
                foreach (Match chain in rawChains)//for each chain, capture the condition and the code block
                {
                    string rawChain = chain.Value;//reprocess the chain to get the condition and code block
                    MatchCollection macroCollection = Regex.Matches(rawChain,
                        @"\((?<keyword>if|else|else-?if):(?<conditionBool>[^\(:\)]+)\) *\[(?<codeBlock>[^\[\]]*)\]",
                        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
                    _InsertCounter++;
                    AddLine($"string insert{_InsertCounter} = string.Empty;\n");//print 1x insert variable for each chain
                    foreach (Match macroHookPair in macroCollection)
                    {
                        GroupCollection macroGroups = macroHookPair.Groups;
                        string keyword = macroGroups[nameof(keyword)].Value;
                        string conditionBool = macroGroups[nameof(conditionBool)].Value;
                        string codeBlock = macroGroups[nameof(codeBlock)].Value;
                        conditionBool = ProcessValueStatement(conditionBool);
                        string branchStatement = ProcessBranchingStatement(keyword, conditionBool);
                        codeBlock = ProcessVariableInString(codeBlock);//NO macro inside the macro hook pls
                        AddLine($"{branchStatement}\ninsert{_InsertCounter} = \"{codeBlock}\";\n");
                    }
                    input = input.Replace(rawChain, $"{{insert{_InsertCounter}}}");
                }
            }
            //else
            //{
            //    MatchCollection macroChains = Regex.Matches(input, @"\(\w+-?\w+:.*\)", RegexOptions.IgnoreCase);
            //    foreach (Match chain in macroChains)
            //    {
            //        string rawChain = chain.Value;
            //        input = input.Replace(rawChain, $"{{{ProcessCStatement(rawChain)}}}");
            //    }
            //}
        }
        return input;
        //return ProcessVariableInString(input);
    }
    #endregion
    #region Format Stuff
    List<string> MarkOutInternalBlock(string topLine)
    {
        //TODO link and hook brackets on the same line how?
        string topLineToCheck = RemovePairedHooksOnLine(topLine);
        if (Regex.IsMatch(topLineToCheck, @"^\[=.*"))
        {
            Match inHook = Regex.Match(topLine, @"^\[=(?<singleLine>.*)$", RegexOptions.ExplicitCapture);
            string singleLine = inHook.Groups[nameof(singleLine)].Value;
            return MarkOutOpenHook(singleLine);
        }
        else if (Regex.IsMatch(topLine, @"^\[.*\]$"))
        {
            Match inHook = Regex.Match(topLine, @"^\[(?<singleLine>.*)\]$", RegexOptions.ExplicitCapture);
            string singleLine = inHook.Groups[nameof(singleLine)].Value;
            return new List<string> { singleLine };
        }
        else if (Regex.IsMatch(RemovePairedHooksOnLine(_RawLines[0]), @"^\[.*\]$"))//if the next line is a single line wrapped block
        {
            string nextLine = DrawFetchLine();
            Match inHook = Regex.Match(nextLine, @"^\[(?<singleLine>.*)\]$", RegexOptions.ExplicitCapture);
            string singleLine = inHook.Groups[nameof(singleLine)].Value;
            return new List<string> { singleLine };
        }
        else
        {//multi line hook MUST start at the next line
            List<string> toReturn = new List<string>();
            for (int h = 0; _RawLines.Count > 0;)
            {
                string nextLine = DrawFetchLine();
                string lineWithRemovedHookPairs = RemovePairedHooksOnLine(nextLine);
                Match inHook;
                if (Regex.IsMatch(lineWithRemovedHookPairs, @"^\["))
                {
                    inHook = Regex.Match(nextLine, @"^\[(?<block>.*)$", RegexOptions.ExplicitCapture);
                    toReturn.Add(inHook.Groups["block"].Value);
                    h++;
                }
                else if (Regex.IsMatch(lineWithRemovedHookPairs, @"\]$"))
                {
                    inHook = Regex.Match(nextLine, @"(?<block>.*)\]$", RegexOptions.ExplicitCapture);
                    toReturn.Add(inHook.Groups["block"].Value);
                    h--;
                    if (h == 0)
                        break;
                }
                else
                    toReturn.Add(nextLine);
            }
            return toReturn;
        }
    }
    string RemovePairedHooksOnLine(string line)
    {
        bool pairedHook;
        string lineToReturn = line;
        do
        {
            pairedHook = Regex.IsMatch(lineToReturn, @"\[([^\[\]]*)\]");
            if (pairedHook)
                lineToReturn = Regex.Replace(lineToReturn, @"\[([^\[\]]*)\]", "");
        }
        while (pairedHook);
        return lineToReturn;
    }
    List<string> MarkOutOpenHook(string topLine)
    {
        List<string> toReturn = _RawLines;
        _RawLines.Clear();
        toReturn.Insert(0, topLine);
        return toReturn;
    }
    bool IsMarkedText(string lineToProcess)
    {
        return Regex.IsMatch(lineToProcess, "^\\(text-style: *\"mark\" *\\) *\\[[^\\[\\]]+\\]$", RegexOptions.IgnoreCase);
    }
    bool TryProcessClickFunc(List<string> lambdaBlock, out string output)
    {
        if (lambdaBlock.Count == 1)
            return TryProcessClickFunc(lambdaBlock[0], out output);
        output = string.Empty;
        return false;
    }
    bool TryProcessClickFunc(string input, out string output)
    {
        if (Regex.IsMatch(input, @"^\(restart: *\)$", RegexOptions.IgnoreCase))
        {
            output = "Restart";
            return true;
        }
        //TODO : Add more click functions
        output = string.Empty;//what else could it be?
        return false;
    }
    bool ContainsVariable(string input)
    {
        return Regex.IsMatch(input, @"\$(\w+)");
    }
    string ProcessVariable(string input)
    {
        if (ContainsVariable(input))
            input = Regex.Replace(input, @"\$(?<variableName>\w+)", "Get(\"$1\")", RegexOptions.ExplicitCapture);
        return input;
    }
    string ProcessVariableInString(string input)
    {
        if (ContainsVariable(input))
            input = Regex.Replace(input, @"\$(?<variableName>\w+)", "{Get(\"$1\")}", RegexOptions.ExplicitCapture);
        return input;
    }
    string ProcessBranchingStatement(string keyword, string conditionBool)
    {
        keyword.Trim();
        keyword.ToLower();
        switch (keyword)
        {
            case "if":
                return $"if({conditionBool})";
            case "else":
                return keyword;
        }
        if (Regex.IsMatch(keyword, @"^else-?if"))
            return $"else if({conditionBool})";
        return keyword;
    }
    string ProcessValueStatement(string rawStatement)
    {
        List<string> fetches = new List<string>();
        //process brackets
        while (Regex.IsMatch(rawStatement, @"\(([^\(\)]+)\)"))
        {
            MatchCollection matchCollection = Regex.Matches(rawStatement, @"\((?<inBracket>[^\(\)]+)\)", RegexOptions.ExplicitCapture);
            foreach (Match m in matchCollection)
            {
                GroupCollection g = m.Groups;
                string inBracket = g[nameof(inBracket)].Value;
                string toReplace = m.Value;
                toReplace = toReplace.Trim(); 
                rawStatement = rawStatement.Replace(m.Value, $"~{fetches.Count}~");
                fetches.Add(inBracket);
            }
        }
        rawStatement = ProcessOperatorKeywords(rawStatement);
        for (int i = fetches.Count; i > 0;)
        {
            i--;
            string processed = ProcessOperatorKeywords(fetches[i]);
            rawStatement = rawStatement.Replace($"~{i}~", $"({processed})");
        }
        return rawStatement;
    }
    string ProcessOperatorKeywords(string rawStatement)
    {
        rawStatement = rawStatement.Trim();
        if (Regex.IsMatch(rawStatement, @"^either:(.+)", RegexOptions.IgnoreCase))
            rawStatement = Regex.Replace(rawStatement, @"^either:(.+)", "Either($1)", RegexOptions.IgnoreCase);
        else if (Regex.IsMatch(rawStatement, @"^history: *"))
            rawStatement = Regex.Replace(rawStatement, @"^history: *", "History", RegexOptions.IgnoreCase);
        if (Regex.IsMatch(rawStatement, @"where +its +name", RegexOptions.IgnoreCase))
        {
            MatchCollection matchCollection = Regex.Matches(rawStatement, @" *where +its +name(?<lambdaStatement>.+)", RegexOptions.IgnoreCase);
            foreach (Match m in matchCollection)
            {
                GroupCollection g = m.Groups;
                string lambdaStatement = g[nameof(lambdaStatement)].Value;
                lambdaStatement = ProcessOperatorKeywords(lambdaStatement);
                rawStatement = rawStatement.Replace(m.Value, $".Where(x => x{lambdaStatement})");
            }
        }
        if (Regex.IsMatch(rawStatement, @"(.+) *(contains|does *not *contain) *(.*) *", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase))
        {//filter out Contains and Does Not Contain
            MatchCollection matchCollection = Regex.Matches(rawStatement, @"(?<prevObject>.+) *(?<containsOrNot>contains|does *not *contain) *(?<compareObject>.*) *", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
            foreach (Match m in matchCollection)
            {
                GroupCollection g = m.Groups;
                string prevObject = g[nameof(prevObject)].Value;
                prevObject = prevObject.Trim();
                string containsOrNot = g[nameof(containsOrNot)].Value;
                string compareObject = g[nameof(compareObject)].Value;
                rawStatement = rawStatement.Replace(m.Value, $"{prevObject}.Contains({compareObject})");
                if (containsOrNot != "contains")
                    rawStatement = "!" + rawStatement;
            }
        }
        rawStatement = Regex.Replace(rawStatement, @" +or +", "||");
        rawStatement = Regex.Replace(rawStatement, @" +and +", "&&");
        rawStatement = Regex.Replace(rawStatement, @" +is +", "==");
        rawStatement = Regex.Replace(rawStatement, @" +is +not +", " != ");
        if(ContainsVariable(rawStatement))
            rawStatement = ProcessVariable(rawStatement);
        return rawStatement;
    }
    bool IsLinkHook(string input)
    {
        return Regex.IsMatch(input, @"^\[\[.+\]\]$");
    }
    bool IsMacro(string rawLine)
    {
        return Regex.IsMatch(rawLine, @"^\(\w+-?\w+:.*\)");
    }
    #endregion
}
#endif