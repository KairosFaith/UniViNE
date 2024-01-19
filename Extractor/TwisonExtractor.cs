using System.Collections.Generic;
using UnityEngine;
using Vine;
using System.IO;
using System;
using System.Text.RegularExpressions;
public class TwisonExtractor : MonoBehaviour
{
    [TextArea(0, 100)]
    public string TwisonOutput;
    void Start()
    {
        twisonStory rawstory = JsonUtility.FromJson<twisonStory>(TwisonOutput);
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
        string className = rawstory.name.RemoveIllegalClassCharacters();
        string path = Application.dataPath + $"/CodeStory/{className}.cs";
        File.AppendAllText(path, "using System.Collections.Generic;\nusing Vine;\n");
        File.AppendAllText(path, $"public class {className} : VineStory\n{{\n");
        //create file
        File.AppendAllText(path, $"public override string StoryName => \"{rawstory.name}\";\n");
        File.AppendAllText(path, $"public override string StartPassage => \"{startPassageName}\";\n");
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
            string lineToWrite = $"{{\"{metadata.Name}\", new VinePassageMetadata(){{ Name = \"{metadata.Name}\", ID = {metadata.ID} {tagsField} }} }},\n";
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
            string nomorewhitespace = rawLine.Replace(" ", "");
            nomorewhitespace = nomorewhitespace.ToLower();
            if (rawLine.StartsWith("#"))
            {
                string header = rawLine.Remove(0,1);//remove the # at the start
                i++;
                string body = rawLines[i];
                processedPassageText += ($"yield return new VineHeaderOutput(\"{header}\", \"{body}\");") + "\n";
            }
            else if (rawLine.StartsWith("("))
            {
                if (nomorewhitespace.StartsWith("(text-style:\"mark\")"))
                {
                    string curLine = rawLine;
                    int textStart = curLine.IndexOf('[') + 1;
                    int textEnd = curLine.IndexOf(']');
                    string text = curLine.Substring(textStart, textEnd - textStart).Trim();
                    text = text.Replace(",", "\",\"");//gotta put some " " between the ,
                    processedPassageText += ($"yield return new UniVineMarkedOutput(\"{text}\");") + "\n";
                }
                //is event macro
                else if (rawLine.StartsWith("(event:whentime>"))
                {
                    //remove (event:whentime> from the front
                    string curLine = rawLine;
                    int textStart = curLine.IndexOf("(event:whentime>") + 16;
                    int textEnd = curLine.Length - 1;
                    string text = curLine.Substring(textStart, textEnd - textStart).Trim();

                    //check for float or int value in front
                    string[] parts = text.Split('s');
                    string time = parts[0];

                    //check for event name (go-to: 
                    string eventName = parts[1].Substring(6);
                    processedPassageText += ($"yield return new VineEventOutput({time}f, \"{eventName}\");") + "\n";
                }
            }
            else if (nomorewhitespace.StartsWith("[[") && nomorewhitespace.EndsWith("]]"))
            {
                //passage link
                string curLine = rawLine;
                int textStart = curLine.IndexOf("[[") + 2;
                int textEnd = curLine.IndexOf("]]");
                string text = curLine.Substring(textStart, textEnd - textStart).Trim();
                text = text.Replace("|", "\",\"");
                processedPassageText += ($"yield return new VineLinkOutput(\"{text}\");") + "\n";
            }
            else if (rawLine.Contains(":"))
            {
                string curLine = rawLine;
                //split by : and remove the space
                curLine = FilterItalic(curLine);
                curLine = FilterColorTag(curLine);
                string[] parts = curLine.Split(':');
                string character = parts[0].Trim();

                string text = string.Empty;
                for(int j = 1; j<parts.Length;j++)
                    text += parts[j].Trim();
                //check for emotion
                if (character.Contains("(") && character.Contains(")"))
                {
                    string emotion = character.Substring(character.IndexOf("(") + 1, character.IndexOf(")") - character.IndexOf("(") - 1);
                    emotion = emotion.ToLower();
                    character = character.Substring(0, character.IndexOf("(")).Trim();//check this
                    processedPassageText += ($"yield return new VineLineOutput(\"{character}\", UniVineCharacterEmotion.{emotion}, \"{text}\");") + "\n";//TODO use enum or string?
                }
                else
                {
                    processedPassageText += ($"yield return new VineLineOutput(\"{character}\", \"{text}\");") + "\n";
                }
            }
            else throw new Exception("Unrecognized line: " + rawLine);
        }
            //processedPassageText += ProcessRawLine(rawLine) + "\n";
        return processedPassageText;
    }
    public string FilterItalic(string input)//from ChatGPT
    {
        // Define the regular expression pattern
        // Create a Regex object
        Regex regex = new Regex(@"\/\/([^\/]+)\/\/");
        // Use Regex.Replace to replace the matched pattern with the HTML italic tags
        string result = regex.Replace(input, "<i>$1</i>");
        return result;
    }
    public string FilterColorTag(string input)
    {
        // Define the regular expression pattern
        // Create a Regex object
        Regex regex = new Regex(@"\(\s*text-colour\s*:\s*([^\)]+)\)\[([^\]]+)\]");

        // Use Regex.Replace to replace the matched pattern with the HTML color tags
        string result = regex.Replace(input, "<color=$1>$2</color>");

        return result;
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