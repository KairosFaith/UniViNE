using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vine;
using System.IO;
using System;
using System.Text.RegularExpressions;
public class TwisonExtractor : MonoBehaviour
{
    public string JSON_Metadata;
    [TextArea(0, 100)]
    public string TwisonOutput;
    void Start()
    {
        twisonStory rawstory = JsonUtility.FromJson<twisonStory>(TwisonOutput);
        VineStoryMetadata metadata = new VineStoryMetadata();
        metadata.StoryName = rawstory.name;
        twisonPassage[] arrayOfRawPassages = rawstory.passages;
        int numberOfPassages = arrayOfRawPassages.Length;
        List<VinePassageMetadata> listOfPassageMetadata = new List<VinePassageMetadata>();
            //id mismatch, use list instead, add to list then convert to array
        int startnode = rawstory.startnode;
        Dictionary<int, string> rawPassageTexts = new Dictionary<int, string>();
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
                metadata.StartPassage = passageName;
        }
        metadata.Data = listOfPassageMetadata.ToArray();
        string className = metadata.StoryName;
        //remove illegal characters from class name using regex
        className = Regex.Replace(className, "[^a-zA-Z0-9_]+", "", RegexOptions.Compiled);
        //TODO I want to go and learn regex

        //create file
        string path = Application.dataPath + $"/CodeStory/{className}.cs";
        File.AppendAllText(path, "using System.Collections.Generic;\nusing Vine;\n");
        File.AppendAllText(path, $"public class {className} : VineStory\n{{\n");
        //front string

        //TODO double check this, need testing
        JSON_Metadata = JsonUtility.ToJson(metadata,true);
        File.AppendAllText(path, $"public override string JSON_Metadata =>");
        string[] lines = JSON_Metadata.Split(new[] { "\n" }, StringSplitOptions.None);
        foreach (string line in lines)
        {
            string processedLine = line.Replace("\"", "\\\"");
            File.AppendAllText(path, "\"" + processedLine + "\"" + "+\n");
        }
        File.AppendAllText(path, "\"\";\n");
        for (int i = 1; i <= numberOfPassages; i++)
        {
            string passageName = metadata.Data[i - 1].Name;//id mismatch
            string passageText = ProcessRawPassageOutput(rawPassageTexts[i]);
            string passageFunction = $"public IEnumerator<VinePassageOutput> Passage{i}()//{passageName}\n{{\n{passageText}\n}}";
            File.AppendAllText(path, passageFunction);
        };
        File.AppendAllText(path, "}");
    }
    string ProcessRawPassageOutput(string rawPassageText)
    {
        string[] rawLines = rawPassageText.Split(new[] { "\n" }, StringSplitOptions.None);
        string processedPassageText = "";
        //foreach (string rawLine in rawLines)
        for(int i = 0;i<rawLines.Length;i++)
        {
            string rawLine = rawLines[i];
            string nomorewhitespace = rawLine.Replace(" ", "");
            nomorewhitespace = nomorewhitespace.ToLower();
            if (rawLine.StartsWith("#"))
            {
                string header = rawLine.Remove(0);//remove the # at the start
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
                string[] parts = curLine.Split(':');
                string character = parts[0].Trim();
                string text = parts[1].Trim();
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