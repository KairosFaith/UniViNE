using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vine;
using System.IO;
using System;
using System.Text.RegularExpressions;
public class TwisonExtractor : MonoBehaviour
{
    public twisonStory rawstory;
    [TextArea(0, 100)]
    public string TwisonOutput;
    string ProcessRawLine(string rawLine)
    {












        return rawLine;
    }
    void Start()
    {
        rawstory = JsonUtility.FromJson<twisonStory>(TwisonOutput);
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
        string path = Application.dataPath + $"/{className}.cs";
        File.AppendAllText(path, "using System.Collections.Generic;\nusing Vine;\n");
        File.AppendAllText(path, $"public class {className} : VineStory\n{{\n");
        //front string
        string JSON_Metadata = JsonUtility.ToJson(metadata);
        JSON_Metadata = "\"" + JSON_Metadata.Replace("\"", "\\\"") + ";\"";
        File.AppendAllText(path, $"public override string JSON_Metadata => {JSON_Metadata};\n");
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
        foreach (string rawLine in rawLines)
            processedPassageText += ProcessRawLine(rawLine) + "\n";
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