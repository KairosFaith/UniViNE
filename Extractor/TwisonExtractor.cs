using System.Collections.Generic;
using UnityEngine;
using Vine;
using System.IO;
using System;
using System.Text.RegularExpressions;
using UnityEditor;
[CreateAssetMenu(fileName = "TwisonExtractor", menuName = "UniViNE/TwisonExtractor", order = 1)]
public class TwisonExtractor : ScriptableObject
{
    [TextArea(0, 100)]
    public string TwisonOutput;
    public void Extract()
    {
        twisonStory rawstory = JsonUtility.FromJson<twisonStory>(TwisonOutput);
        string className = rawstory.name.RemoveIllegalClassCharacters();
        string path = Application.dataPath + $"/CodeStory/{className}.cs";
        if (File.Exists(path))
            File.Delete(path);
        twisonPassage[] arrayOfRawPassages = rawstory.passages;
        int numberOfPassages = arrayOfRawPassages.Length;
        List<VinePassageMetadata> listOfPassageMetadata = new List<VinePassageMetadata>();
        int startnode = rawstory.startnode;
        Dictionary<int, string> rawPassageTexts = new Dictionary<int, string>();
        string startPassageName = string.Empty;
        foreach (twisonPassage passage in arrayOfRawPassages)
        {
            int passageID = passage.pid;
            string passageName = passage.name;
            VinePassageMetadata passageMetadata = new VinePassageMetadata
            {
                ID = passageID,
                Name = passageName,
                Tags = passage.tags
            };
            listOfPassageMetadata.Add(passageMetadata);
            rawPassageTexts[passageID] = passage.text;
            if (passageID == startnode)
                startPassageName = passageName;
        }
        File.AppendAllText(path, $"using System.Collections.Generic;\nusing System.Linq;\nusing {nameof(Vine)};//You need the script VineExMachina or nothing will work\n");
        File.AppendAllText(path, $"public class {className} : {nameof(VineStory)}\n{{\n");
        File.AppendAllText(path, $"public override string {nameof(VineStory.StoryName)} => \"{rawstory.name}\";\n");
        File.AppendAllText(path, $"public override string {nameof(VineStory.StartPassage)} => \"{startPassageName}\";\n");
        File.AppendAllText(path, "public override Dictionary<string, VinePassageMetadata> Passages => new Dictionary<string, VinePassageMetadata>() {\n");
        foreach (VinePassageMetadata metadata in listOfPassageMetadata)
        {
            string tagsField = string.Empty;
            if (metadata.Tags != null)
            {
                tagsField = ", Tags = new string[] {";
                foreach (string tag in metadata.Tags)
                    tagsField += $"\"{tag}\",";
                tagsField += "}";
            }
            string lineToWrite = $"{{\"{metadata.Name}\", new {nameof(VinePassageMetadata)}(){{ Name = \"{metadata.Name}\", ID = {metadata.ID} {tagsField} }} }},\n";
            File.AppendAllText(path, lineToWrite);
        }
        File.AppendAllText(path, "};\n");

        for (int i = 1; i <= numberOfPassages; i++)
        {
            string passageName = listOfPassageMetadata[i - 1].Name;//id mismatch
            try
            {
                string passageText = ProcessHarloweRawPassageOutput(rawPassageTexts[i]);
                string passageFunction = $"public IEnumerator<VinePassageOutput> Passage{i}()//{passageName}\n{{\n{passageText}\n}}\n";
                File.AppendAllText(path, passageFunction);
            }
            catch(Exception e)
            {
                Debug.Log(e);
                Debug.Log(passageName + "Passage Number: " + i);
                //File.Delete(path);
                //return;
            }
        };
        File.AppendAllText(path, "}");
    }
    public string ProcessHarloweRawPassageOutput(string rawPassageText)
    {
        string[] rawLines = rawPassageText.Split(new[] { "\n" }, StringSplitOptions.None);
        string processedPassageText = string.Empty;
        for (int passageLineCount = 0, insertCount = 0 ;passageLineCount<rawLines.Length; passageLineCount++)
        {
            string rawLine = rawLines[passageLineCount];
            rawLine = rawLine.Trim();
            string ProcessMacrosInString(string input)
            {
                if (Regex.IsMatch(input, @"\(\w+-?\w+:.*\)"))
                {
                    if (Regex.IsMatch(input, @"\((if|else|else-?if):.+\) *\[.*\]", RegexOptions.IgnoreCase))//check inline branching
                    {
                        MatchCollection rawChains = Regex.Matches(input, @"\((if|else|else-?if):.+\) *\[.*\]", RegexOptions.IgnoreCase);//capture the whole branch macro chain
                        foreach (Match chain in rawChains)//for each chain, capture the condition and the code block
                        {
                            string rawChain = chain.Value;//reprocess the chain to get the condition and code block
                            MatchCollection macroCollection = Regex.Matches(rawChain, @"\((?<keyword>if|else|else-?if):(?<conditionBool>.+)\) *\[(?<codeBlock>[^\[\]]*)\]", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
                            processedPassageText += $"string insert{insertCount} = string.Empty;\n";//print 1x insert variable for each chain
                            foreach (Match macroHookPair in macroCollection)
                            {
                                GroupCollection macroGroups = macroHookPair.Groups;
                                string keyword = macroGroups[nameof(keyword)].Value;
                                string conditionBool = macroGroups[nameof(conditionBool)].Value;
                                string codeBlock = macroGroups[nameof(codeBlock)].Value;
                                conditionBool = ProcessStatement(conditionBool);
                                string branchStatement = ProcessBranchingStatement(keyword, conditionBool);
                                codeBlock = ProcessVariableInString(codeBlock);
                                processedPassageText += $"{branchStatement}\ninsert{insertCount} = \"{codeBlock}\"\n";
                            }
                            input = input.Replace(rawChain, $"{{insert{insertCount}}}");
                            insertCount++;
                        }
                    }
                    else if (false)//TODO other lambda line macros
                    {

                    }
                    else
                    {
                        MatchCollection macroChains = Regex.Matches(input, @"\(\w+-?\w+:.*\)", RegexOptions.IgnoreCase);
                        foreach (Match chain in macroChains)
                        {
                            string rawChain = chain.Value;
                            input = input.Replace(rawChain, $"{{{ProcessStatement(rawChain)}}}");
                        }
                    }
                }
                input = ProcessVariableInString(input);
                return input;
            };
            if (Regex.IsMatch(rawLine, @"^\(\w+-?\w+:.*\)"))
            {//Is Line a Macro?
                if (Regex.IsMatch(rawLine, @"^\((if|else|else-?if):.*\)", RegexOptions.IgnoreCase))
                {//is branching macro
                    string keyword, conditionBool, codeBlock;
                    if (Regex.IsMatch(rawLine, @"^\((if|else|else-?if):.*\) *\[.+\]$", RegexOptions.IgnoreCase))
                    {//if its a single line wrapped macro
                        Match m = Regex.Match(rawLine, @"^\((?<keyword>if|else|else-?if):(?<conditionBool>.*)\) *\[(?<codeBlock>.+)\]$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
                        GroupCollection g = m.Groups;
                        keyword = g[nameof(keyword)].Value;
                        conditionBool = g[nameof(conditionBool)].Value;
                        codeBlock = g[nameof(codeBlock)].Value;
                    }
                    else if (Regex.IsMatch(rawLine, @"^\((if|else|else-?if):.*\) *\[=", RegexOptions.IgnoreCase))
                    {//using open hook, must be on the same line as the macro 
                        Match m = Regex.Match(rawLine, @"^\((?<keyword>if|else|else-?if):(?<conditionBool>.+)\) *\[=(?<codeBlock>.*)$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        GroupCollection g = m.Groups;
                        keyword = g[nameof(keyword)].Value;
                        conditionBool = g[nameof(conditionBool)].Value;
                        codeBlock = g[nameof(codeBlock)].Value;
                        while (passageLineCount < rawLines.Length)
                        {
                            codeBlock += rawLines[passageLineCount];
                            passageLineCount++;
                        }
                    }
                    else
                    {//multi line wrapped text, hooks must be at the start and end of the chunk
                        Match m = Regex.Match(rawLine, @"^\((?<keyword>if|else|else-?if):(?<conditionBool>.*)\) *$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
                        keyword = m.Groups[nameof(keyword)].Value;
                        conditionBool = m.Groups[nameof(conditionBool)].Value;
                        codeBlock = string.Empty;

                        //TODO exception handling for links
                        passageLineCount++;
                        rawLine = rawLines[passageLineCount];
                        string lineToCheck = RemoveLinkHooks(rawLine);
                        if (Regex.IsMatch(lineToCheck, @"^\[.*\]$"))
                        {//only one line below
                            Match inHook = Regex.Match(rawLine, @"^\[(?<codeBlock>.*)\]$", RegexOptions.ExplicitCapture);
                            codeBlock += inHook.Groups[nameof(codeBlock)].Value;
                        }
                        else //multi line wrapped
                            for (int h = 0; passageLineCount < rawLines.Length; passageLineCount++)
                            {
                                string nextRawLine = rawLines[passageLineCount];
                                lineToCheck = RemoveLinkHooks(nextRawLine);
                                if (Regex.IsMatch(lineToCheck, @"^\[.*"))
                                {
                                    Match inHook = Regex.Match(nextRawLine, @"^\[(?<codeBlock>.*)", RegexOptions.ExplicitCapture);
                                    codeBlock += inHook.Groups[nameof(codeBlock)].Value + "\n";
                                    h++;
                                }
                                else if (Regex.IsMatch(lineToCheck, @"\]$"))
                                {
                                    Match inHook = Regex.Match(nextRawLine, @"^(?<codeBlock>.*)\]$", RegexOptions.ExplicitCapture);
                                    codeBlock += inHook.Groups[nameof(codeBlock)].Value + "\n";
                                    h--;
                                    if (h == 0)
                                        break;
                                }
                                else
                                    codeBlock += nextRawLine + "\n";
                            }
                    }
                    conditionBool = ProcessStatement(conditionBool);
                    string branchStatement = ProcessBranchingStatement(keyword, conditionBool);
                    codeBlock = ProcessHarloweRawPassageOutput(codeBlock);
                    processedPassageText += $"{branchStatement}\n{{{codeBlock}}}\n";
                }
                else if (Regex.IsMatch(rawLine, "^\\(text-style: *\"mark\" *\\) *\\[[^\\[\\]]+\\]$", RegexOptions.IgnoreCase))
                {
                    Match t = Regex.Match(rawLine, @"\[(?<messageWithArguments>.+)\]$", RegexOptions.ExplicitCapture);
                    string messageWithArguments = t.Groups[nameof(messageWithArguments)].Value;
                    messageWithArguments = ProcessMacrosInString(messageWithArguments);
                    messageWithArguments = Regex.Replace(messageWithArguments, ", *", "\", \"");
                    processedPassageText += $"yield return new {nameof(UniVineMarkedOutput)}(\"{messageWithArguments}\");" + "\n";
                }
                else if (IsDelayedLink(rawLine))
                {
                    Match t = Regex.Match(rawLine, "^\\(event: *when +time *>=? *(?<time>\\d*\\.?\\d)s *\\) *\\[=?\\(go-to: *\"(?<passageName>.+)\"", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                    GroupCollection m = t.Groups;
                    string time = m[nameof(time)].Value;
                    string passageName = m[nameof(passageName)].Value;
                    processedPassageText += $"yield return new {nameof(VineDelayLinkOutput)}({time}, \"{passageName}\");" + "\n";
                }
                else
                    processedPassageText += ProcessStatement(rawLine) + ";\n";
                //processedPassageText += ProcessStatement(rawLine) + "\n";
            }
            else if (rawLine.StartsWith("#"))//Check header
            {
                string header = rawLine.Remove(0,1);//remove the # at the start
                passageLineCount++;
                string body = rawLines[passageLineCount];
                processedPassageText += $"yield return new {nameof(VineHeaderOutput)}(\"{header}\", \"{body}\");" + "\n";
            }
            else if (IsLinkHook(rawLine))
            {
                Match l = Regex.Match(rawLine, @"^\[\[(?<labelWithPassageName>.+)\]\]$", RegexOptions.ExplicitCapture);
                string labelWithPassageName = l.Groups[nameof(labelWithPassageName)].Value;
                labelWithPassageName = labelWithPassageName.Replace("|", "\",\"");
                processedPassageText += ($"yield return new {nameof(VineLinkOutput)}(\"{labelWithPassageName}\");") + "\n";
            }
            else if (rawLine.Contains(":"))//assume it's a dialogue line since its not a macro and doesn't start with a #
            {
                rawLine = Regex.Replace(rawLine, @"//([^//]+)//", @"<i>$1</i>");
                rawLine = Regex.Replace(rawLine, @"\(text-colour: *(?<color>[A-Za-z]+) *\) *\[(?<text>[^\[\]]+)\]", @"<color=$1>$2</color>", RegexOptions.ExplicitCapture);
                ProcessMacrosInString(rawLine);
                if (Regex.IsMatch(rawLine, @"^([^:\(\)]+)\(([A-Za-z]+)\): ?(.+)$"))
                {
                    Match t = Regex.Match(rawLine, @"^(?<character>[^:\(\)]+)\((?<emotion>[A-Za-z]+)\): ?(?<speech>.+)$", RegexOptions.ExplicitCapture);
                    GroupCollection m = t.Groups;
                    string character = m[nameof(character)].Value;
                    VineCharacterEmotion emotion = (VineCharacterEmotion)Enum.Parse(typeof(VineCharacterEmotion), m[nameof(emotion)].Value.ToLower());
                    string speech = m[nameof(speech)].Value.Trim();
                    processedPassageText += $"yield return new {nameof(VineLineOutput)}(\"{character}\", {nameof(VineCharacterEmotion)}.{emotion}, $\"{speech}\");" + "\n";
                }
                else if (Regex.IsMatch(rawLine, @"([^:]+): *(.+)"))
                {
                    rawLine = Regex.Replace(rawLine, @"(?<character>[^:]+): *(?<speech>.+)", "$1\", $\"$2");//TODO maybe use match compose instead?
                    processedPassageText += $"yield return new {nameof(VineLineOutput)}(\"{rawLine}\");" + "\n";
                }
                else if (!string.IsNullOrWhiteSpace(rawLine))
                    throw new Exception(passageLineCount + rawLine + "Dialogue Line Format is Incorrect");
            }
            else if(!string.IsNullOrWhiteSpace(rawLine))
                throw new Exception(passageLineCount + "Could not match any line format" + rawLine);
        }
        return processedPassageText;
    }
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
    string ProcessVariableInString(string input)
    {
        if(Regex.IsMatch(input, @"\$(\w+)"))
            input = Regex.Replace(input, @"\$(?<variableName>\w+)", "{Get(\"<$1>\")}");
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
        if(Regex.IsMatch(keyword, @"^else-?if"))
            return $"else if({conditionBool})";
        return keyword;
    } 
    string ProcessStatement(string rawStatement)
    {
        List<string> fetches = new List<string>();
        if (Regex.IsMatch(rawStatement, @"^\(set: *\$(.+) to (.+)\)$", RegexOptions.IgnoreCase))
            rawStatement = Regex.Replace(rawStatement, @"^\(set: *\$(?<variableToSet>.+) to (?<valueStatement>.+)\)$", "Set[\"$1\"] = $2", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
        while (Regex.IsMatch(rawStatement, @"\(([^\(\)]*)\)"))
        {
            MatchCollection matchCollection = Regex.Matches(rawStatement, @"\((?<inBracket>[^\(\)]*)\)", RegexOptions.ExplicitCapture);
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
        if(Regex.IsMatch(rawStatement, @"^either:(.+)", RegexOptions.IgnoreCase))
            rawStatement = Regex.Replace(rawStatement, @"^either:(.+)", "Either($1)", RegexOptions.IgnoreCase);
        else if(Regex.IsMatch(rawStatement, @"^history: *"))
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
[Serializable]
public class twisonStory
{
    public twisonPassage[] passages;
    public string name;
    public int startnode;
}
[Serializable]
public class twisonPassage
{
    public string text, name;
    public int pid;
    public string[] tags;
}
[CustomEditor(typeof(TwisonExtractor))]
public class TwisonExtractorDrawer : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Extract"))
        {
            TwisonExtractor extractor = (TwisonExtractor)target;
                extractor.Extract();
        }
        DrawDefaultInspector();
    }
}