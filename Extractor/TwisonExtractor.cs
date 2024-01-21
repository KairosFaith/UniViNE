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
            VinePassageMetadata passageMetadata = new VinePassageMetadata();
            int passageID = passage.pid;
            string passageName = passage.name;
            passageMetadata.ID = passageID;
            passageMetadata.Name = passageName;
            passageMetadata.Tags = passage.tags;
            listOfPassageMetadata.Add(passageMetadata);
            rawPassageTexts[passageID] = passage.text;
            if (passageID == startnode)
                startPassageName = passageName;
        }
        File.AppendAllText(path, "using System.Collections.Generic;\nusing Vine;\n");
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
            string passageText = ProcessRawPassageOutput(rawPassageTexts[i]);
            string passageFunction = $"public IEnumerator<VinePassageOutput> Passage{i}()//{passageName}\n{{\n{passageText}\n}}\n";
            File.AppendAllText(path, passageFunction);
        };
        File.AppendAllText(path, "}");
    }
    string ProcessRawPassageOutput(string rawPassageText)//Regex this shit
    {
        string[] rawLines = rawPassageText.Split(new[] { "\n" }, StringSplitOptions.None);
        string processedPassageText = "";
        for(int i = 0;i<rawLines.Length;i++)
        {
            string rawLine = rawLines[i];
            if (rawLine.StartsWith("#"))//Check header
            {
                string header = rawLine.Remove(0,1);//remove the # at the start
                i++;
                string body = rawLines[i];
                processedPassageText += ($"yield return new {nameof(VineHeaderOutput)}(\"{header}\", \"{body}\");") + "\n";
            }
            else if (IsMacro(rawLine))
            {
                if (IsMarkedOutputText(rawLine))
                {
                    Match t = Regex.Match(rawLine, @"\[(.+)\]");
                    string text = t.Groups[1].Value;
                    text = text.Replace(",", "\",\"");
                    processedPassageText += $"yield return new {nameof(UniVineMarkedOutput)}(\"{text}\");" + "\n";
                }
                else if (IsDelayedLink(rawLine))
                {
                    Match t = Regex.Match(rawLine, "^\\(event: *when +time *>=? *(\\d*\\.?\\d)s *\\) *\\[=?\\(go-to: *\"(.+)\"", RegexOptions.IgnoreCase);
                    GroupCollection m = t.Groups;
                    string time = m[1].Value;
                    string passageName = m[1].Value;
                    processedPassageText += $"yield return new {nameof(VineDelayLinkOutput)}({time}, \"{passageName}\");" + "\n";
                }
                else
                    Debug.LogWarning("Unrecognized or unsupported macro: " + rawLine);
            }
            else if (IsLink(rawLine))
            {
                Match l = Regex.Match(rawLine, @"^\[\[+(.+)\]+\]$");
                string text = l.Groups[1].Value;
                text = text.Replace("|", "\",\"");
                processedPassageText += ($"yield return new {nameof(VineLinkOutput)}(\"{text}\");") + "\n";
            }
            else if (rawLine.Contains(":"))//assume it's a dialogue line
            {
                string curLine = rawLine;
                curLine = ConvertIttalics(curLine);
                curLine = ConvertColorTag(curLine);
                if(Regex.IsMatch(curLine, @"^.+\([A-Za-z]\):.+$"))
                {
                    Match t = Regex.Match(curLine, @"^(.+)\(([A-Za-z])\):(.+)$");
                    GroupCollection m = t.Groups;
                    string character = m[1].Value;
                    VineCharacterEmotion emotion = (VineCharacterEmotion)Enum.Parse(typeof(VineCharacterEmotion),m[2].Value);
                    string text = m[3].Value;
                    processedPassageText += $"yield return new {nameof(VineLineOutput)}(\"{character}\", {nameof(VineCharacterEmotion)}.{emotion}, \"{text}\");" + "\n";
                }
                else
                {
                    curLine = curLine.Replace(": ", "\",\"");
                    processedPassageText += ($"yield return new {nameof(VineLineOutput)}(\"{curLine}\");") + "\n";
                }
                //TODO auto trim text or expect whitespace after :
            }
            else throw new Exception("Unrecognized line: " + rawLine);
        }
        return processedPassageText;
    }
    #region REGEX Functions
    bool IsMacro(string input)
    {
        return Regex.IsMatch(input, @"^\(\w+-?\w+:.*\)");
    }
    bool IsMarkedOutputText(string input)
    {
        return Regex.IsMatch(input, "^\\(text-style: *\"mark\" *\\)", RegexOptions.IgnoreCase);
    }
    bool IsDelayedLink(string input)
    {
        if(IsDelayedEvent(input))//TODO: check if this is the best way to do this
            return Regex.IsMatch(input, "\\(go-to: *\".+\" *\\)", RegexOptions.IgnoreCase);//assume go-to macro will not be used for any other purpose
        return false;
    }
    bool IsDelayedEvent(string input)
    {
        return Regex.IsMatch(input, @"^\(event: *when +time *>=? *\d*\.?\ds *\)\[(=|\]$)", RegexOptions.IgnoreCase);
    }
    bool IsLink(string input)
    {
        return Regex.IsMatch(input, @"^(\[\[).*(\]\])$");
    }
    public string ConvertIttalics(string input)
    {
        string output = input;
        if(Regex.IsMatch(input, @"//.+//"))
        {
            MatchCollection matchCollection = Regex.Matches(input, @"//(.+)//");
            foreach (Match match in matchCollection)
                output = output.Replace(match.Value, $"<i>{match.Groups[1].Value}</i>");
        }
        return output;
    }
    #endregion
    public string ConvertColorTag(string input)
    {
        string output = input;
        if (Regex.IsMatch(input, @"^\(text-colour: *[A-Za-z]+ *\) *\[.+\]"))
        {
            MatchCollection matchCollection = Regex.Matches(input, @"^\(text-colour: *([A-Za-z]+) *\) *\[(.+)\]");
            foreach (Match match in matchCollection)
            {
                GroupCollection m = match.Groups;
                string colorTag = m[1].Value;
                string text = m[2].Value;
                output = output.Replace(match.Value, $"<color={colorTag}>{text}</color>");
            }
        }
        return output;
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
            //EditorUtility.
        }
        DrawDefaultInspector();
    }
}