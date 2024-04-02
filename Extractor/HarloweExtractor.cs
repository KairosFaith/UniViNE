using System.Collections;
using Vine;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine;
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
    public override string[] Extract()
    {
        while(_RawLines.Count > 0)
        {
            string nextLineToProcess = DrawFetchLine();
            string processedLine = FilterAndProcessLine(nextLineToProcess);
            _ProcessedLines.Add(processedLine);
        }
        return _ProcessedLines.ToArray();
    }
    public string DrawFetchLine()
    {
        string fetchLine = _RawLines[0];
        _RawLines.RemoveAt(0);
        return fetchLine;
    }
    //main condition thread
    string FilterAndProcessLine(string lineToProcess)
    {
        if (Regex.IsMatch(lineToProcess, @"^\(\w+-?\w+:.*\)"))
        {//Is Line a Macro?
            if (Regex.IsMatch(lineToProcess, @"^\((if|else|else-?if):.*\)", RegexOptions.IgnoreCase))
                return ProcessBranchingMacro(lineToProcess);
            else if (Regex.IsMatch(lineToProcess, "^\\(text-style: *\"mark\" *\\) *\\[[^\\[\\]]+\\]$", RegexOptions.IgnoreCase))
                return ProcessMarkedText(lineToProcess);
            else if (IsDelayedLink(lineToProcess))
                return ProcessDelayedLink(lineToProcess);
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
            lineToProcess = ProcessMacrosInString(lineToProcess);
            if (Regex.IsMatch(lineToProcess, @"^([^:\(\)]+)\(([A-Za-z]+)\): ?(.+)$"))
            {
                Match t = Regex.Match(lineToProcess, @"^(?<character>[^:\(\)]+)\((?<emotion>[A-Za-z]+)\): ?(?<speech>.+)$", RegexOptions.ExplicitCapture);
                GroupCollection m = t.Groups;
                string character = m[nameof(character)].Value;
                VineCharacterEmotion emotion = (VineCharacterEmotion)Enum.Parse(typeof(VineCharacterEmotion), m[nameof(emotion)].Value.ToLower());
                string speech = m[nameof(speech)].Value.Trim();
                return $"yield return new {nameof(VineLineOutput)}(\"{character}\", {nameof(VineCharacterEmotion)}.{emotion}, $\"{speech}\");" + "\n";
            }
            else if (Regex.IsMatch(lineToProcess, @"([^:]+): *(.+)"))
            {
                lineToProcess = Regex.Replace(lineToProcess, @"(?<character>[^:]+): *(?<speech>.+)", "$1\", $\"$2");
                return $"yield return new {nameof(VineLineOutput)}(\"{lineToProcess}\");" + "\n";
            }
        }
        return ProcessCStatement(lineToProcess) + ";\n";
    }
    string ProcessBranchingMacro(string lineToProcess)
    {//is branching macro
        string keyword, conditionBool;
        List<string> codeBlock;
        if (Regex.IsMatch(lineToProcess, @"^\((if|else|else-?if):.*\) *\[.+\]$", RegexOptions.IgnoreCase))
        {//if its a single line wrapped macro
            Match m = Regex.Match(lineToProcess, @"^\((?<keyword>if|else|else-?if):(?<conditionBool>.*)\) *\[(?<codeBlock>.+)\]$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
            GroupCollection g = m.Groups;
            keyword = g[nameof(keyword)].Value;
            conditionBool = g[nameof(conditionBool)].Value;
            codeBlock = new List<string>() { g[nameof(codeBlock)].Value };
        }
        else if (Regex.IsMatch(lineToProcess, @"^\((if|else|else-?if):.*\) *\[=", RegexOptions.IgnoreCase))
        {//using open hook, must be on the same line as the macro 
            Match m = Regex.Match(lineToProcess, @"^\((?<keyword>if|else|else-?if):(?<conditionBool>.+)\) *\[=(?<codeBlock>.*)$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            GroupCollection g = m.Groups;
            keyword = g[nameof(keyword)].Value;
            conditionBool = g[nameof(conditionBool)].Value;
            codeBlock = MarkOutOpenHook(g[nameof(codeBlock)].Value);
        }
        else
        {//multi line wrapped text, hooks must be at the start and end of the chunk
            Match m = Regex.Match(lineToProcess, @"^\((?<keyword>if|else|else-?if):(?<conditionBool>.*)\) *$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
            keyword = m.Groups[nameof(keyword)].Value;
            conditionBool = m.Groups[nameof(conditionBool)].Value;
            codeBlock = MarkOutMultilineWrap();
        }
        conditionBool = ProcessCStatement(conditionBool);
        string branchStatement = ProcessBranchingStatement(keyword, conditionBool);
        _ProcessedLines.Add($"{branchStatement}\n{{");
        ExtractInternalBlock(codeBlock);
        return "}\n";
    }
    string ProcessMarkedText(string lineToProcess)
    {
        Match t = Regex.Match(lineToProcess, @"\[(?<messageWithArguments>.+)\]$", RegexOptions.ExplicitCapture);
        string messageWithArguments = t.Groups[nameof(messageWithArguments)].Value;
        messageWithArguments = ProcessMacrosInString(messageWithArguments);
        messageWithArguments = Regex.Replace(messageWithArguments, ", *", "\", \"");
        return $"yield return new {nameof(UniVineMarkedOutput)}(\"{messageWithArguments}\");" + "\n";
    }
    string ProcessDelayedLink(string lineToProcess)
    {
        Match t = Regex.Match(lineToProcess, "^\\(event: *when +time *>=? *(?<time>\\d*\\.?\\d)s *\\) *\\[=?\\(go-to: *\"(?<passageName>.+)\"", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
        GroupCollection m = t.Groups;
        string time = m[nameof(time)].Value;
        string passageName = m[nameof(passageName)].Value;
        return $"yield return new {nameof(VineDelayLinkOutput)}({time}, \"{passageName}\");" + "\n";
    }
    string ProcessClickLambda(string lineToProcess)
    {
        string textClick;
        List<string> lambdaBlock;
        if (Regex.IsMatch(lineToProcess, "(?<front>.+)\\(click: *\"(?<textClick>.+)\" *\\) *\\[(?<lambdaBlock>.+)\\]$", RegexOptions.IgnoreCase))
        {
            Match m = Regex.Match(lineToProcess, "(?<front>.+)\\(click: *\"(?<textClick>.+)\" *\\) *\\[(?<lambdaLine>.+)\\]$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            GroupCollection g = m.Groups;
            textClick = g[nameof(textClick)].Value;
            string lambdaLine = g[nameof(lambdaLine)].Value;
            if (TryProcessClickFunc(lambdaLine, out string clickFunc))
                return $"yield return new {nameof(VineClickLamdaOutput)}(\"{textClick}\", {clickFunc});" + "\n";
            else
                lambdaBlock = new List<string> { lambdaLine };
        }
        else if (Regex.IsMatch(lineToProcess, "(?<front>.+)\\(click: *\"(?<textClick>.+)\" *\\) *\\[=(?<lambdaBlock>.*)$", RegexOptions.IgnoreCase))
        {
            Match m = Regex.Match(lineToProcess, "(?<front>.+)\\(click: *\"(?<textClick>.+)\" *\\) *\\[=(?<lambdaBlock>.*)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            GroupCollection g = m.Groups;
            textClick = g[nameof(textClick)].Value;
            lambdaBlock = MarkOutOpenHook(g[nameof(lambdaBlock)].Value);
        }
        else
        {//multi line wrapped text, hooks must be at the start and end of the chunk
            Match m = Regex.Match(lineToProcess, "(?<front>.+)\\(click: *\"(?<textClick>.+)\" *\\) *$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
            GroupCollection g = m.Groups;
            textClick = g[nameof(textClick)].Value;
            lambdaBlock = MarkOutMultilineWrap();
        }
        textClick = ProcessMacrosInString(textClick);
        _ProcessedLines.Add($"yield return new {nameof(VineClickLamdaOutput)}(\"{textClick}\", () =>\n {{");
        ExtractInternalBlock(lambdaBlock);
        return "});\n";
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
    void ExtractInternalBlock(List<string> lines)
    {
        foreach(string line in lines)
        {
            string processedLine = FilterAndProcessLine(line);
            _ProcessedLines.Add(processedLine);
        }
    }
    List<string> MarkOutOpenHook(string topLine)
    {
        List<string> toReturn = _RawLines;
        _RawLines.Clear();
        toReturn.Insert(0, topLine);
        return toReturn;
    }
    List<string> MarkOutMultilineWrap()
    {
        List<string> toReturn = new List<string>();
        string lineToCheck = DrawFetchLine();
         lineToCheck = RemoveLinkHooks(lineToCheck);
        if (Regex.IsMatch(lineToCheck, @"^\[.*\]$"))
        {//only one line below
            Match inHook = Regex.Match(lineToCheck, @"^\[(?<block>.*)\]$", RegexOptions.ExplicitCapture);
            toReturn.Add(inHook.Groups["block"].Value);
        }
        else //multi line wrapped
            for (int h = 0; _RawLines.Count > 0;)
            {
                if (Regex.IsMatch(lineToCheck, @"^\[.*"))
                {
                    Match inHook = Regex.Match(lineToCheck, @"^\[(?<block>.*)$", RegexOptions.ExplicitCapture);
                    toReturn.Add(inHook.Groups["block"].Value + "\n");
                    h++;
                }
                else if (Regex.IsMatch(lineToCheck, @"\]$"))
                {
                    Match inHook = Regex.Match(lineToCheck, @"^(?<block>.*)\]$", RegexOptions.ExplicitCapture);
                    toReturn.Add(inHook.Groups["block"].Value + "\n");
                    h--;
                    if (h == 0)
                        break;
                }
                else
                    toReturn.Add(lineToCheck + "\n");
                lineToCheck = DrawFetchLine();
                lineToCheck = RemoveLinkHooks(lineToCheck);
            }
        return toReturn;
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





    #region Parse Handling Functions
    string ProcessMacrosInString(string input)
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
                    _ProcessedLines.Add($"string insert{_InsertCounter} = string.Empty;\n");//print 1x insert variable for each chain
                    foreach (Match macroHookPair in macroCollection)
                    {
                        GroupCollection macroGroups = macroHookPair.Groups;
                        string keyword = macroGroups[nameof(keyword)].Value;
                        string conditionBool = macroGroups[nameof(conditionBool)].Value;
                        string codeBlock = macroGroups[nameof(codeBlock)].Value;
                        conditionBool = ProcessCStatement(conditionBool);
                        string branchStatement = ProcessBranchingStatement(keyword, conditionBool);
                        codeBlock = ProcessVariableInString(codeBlock);//NO macro inside the macro hook pls
                        _ProcessedLines.Add($"{branchStatement}\ninsert{_InsertCounter} = \"{codeBlock}\";\n");
                    }
                    input = input.Replace(rawChain, $"{{insert{_InsertCounter}}}");
                }
            }
            else
            {
                MatchCollection macroChains = Regex.Matches(input, @"\(\w+-?\w+:.*\)", RegexOptions.IgnoreCase);
                foreach (Match chain in macroChains)
                {
                    string rawChain = chain.Value;
                    input = input.Replace(rawChain, $"{{{ProcessCStatement(rawChain)}}}");
                }
            }
        }
        return ProcessVariableInString(input);
    }
    #endregion
    #region REGEX Shortforms
    bool IsLinkHook(string input)
    {
        return Regex.IsMatch(input, @"^\[\[.+\]\]$");
    }
    string RemoveLinkHooks(string lineToCheck)
    {
        if (IsLinkHook(lineToCheck))
            lineToCheck = Regex.Replace(lineToCheck, @"\[\[([^\[\]]+)\]\]", @"");
        return lineToCheck;
    }
    bool IsDelayedLink(string input)
    {
        return
            Regex.IsMatch(input, @"^\(event: *when +time *>=? *\d*\.?\ds *\) *\[(=|.+\]$)", RegexOptions.IgnoreCase)
            && Regex.IsMatch(input, "^\\(event: *when +time *>=? *\\d*\\.?\\ds *\\) *\\[=? *\\(go-to: *\".+\" *\\)", RegexOptions.IgnoreCase)
            ;
    }
    bool IsMacro(string rawLine)
    {
        return Regex.IsMatch(rawLine, @"^\(\w+-?\w+:.*\)");
    }
    bool IsClickLambda(string rawLine)
    {
        if (Regex.IsMatch(rawLine, "(?<front>.+)\\(click: *\"(?<textClick>.+)\" *\\)", RegexOptions.IgnoreCase))
        {
            Match m = Regex.Match(rawLine, "(?<front>.+)\\(click: *\"(?<textClick>.+)\" *\\)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            string front = m.Groups[nameof(front)].Value;
            string textClick = m.Groups[nameof(textClick)].Value;
            return front == textClick;
        }
        return false;
    }
    string ProcessVariableInString(string input)
    {
        if (Regex.IsMatch(input, @"\$(\w+)"))
            input = Regex.Replace(input, @"\$(?<variableName>\w+)", "{Get(\"$1\")}", RegexOptions.ExplicitCapture);
        return input;
    }
    #endregion
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
    string ProcessCStatement(string rawStatement)
    {
        List<string> fetches = new List<string>();
        //Fixed Statement structures
        if (Regex.IsMatch(rawStatement, @"^\(set: *\$(.+) to (.+)\)$", RegexOptions.IgnoreCase))
            rawStatement = Regex.Replace(rawStatement, @"^\(set: *\$(?<variableToSet>.+) to (?<valueStatement>.+)\)$", "Set[\"$1\"] = $2", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
        //Coded Functions
        if (Regex.IsMatch(rawStatement, @"^\(restart: *\)$", RegexOptions.IgnoreCase))
            rawStatement = Regex.Replace(rawStatement, @"\(restart: *\)", "Restart()", RegexOptions.IgnoreCase);
        if (Regex.IsMatch(rawStatement, @"^\(go-to: *(?<passageName>.+) *\)$", RegexOptions.IgnoreCase))
            rawStatement = Regex.Replace(rawStatement, @"^\(go-to: *(?<passageName>.+) *\)$", "GoTo($1)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
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
                //else
                {
                    rawStatement = rawStatement.Replace(m.Value, $"~{fetches.Count}~");
                    fetches.Add(inBracket);
                }
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
        rawStatement = Regex.Replace(rawStatement, @"\$(?<variableName>\w+)", "Get(\"$1\")");
        return rawStatement;
    }
}