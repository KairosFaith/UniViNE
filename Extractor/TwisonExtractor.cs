#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using Vine;
using System.IO;
using System;
using UnityEditor;
[CreateAssetMenu(fileName = "TwisonExtractor", menuName = "UniViNE/TwisonExtractor")]
public class TwisonExtractor : ScriptableObject
{
    [TextArea(30,40)]
    public string TwisonOutput;
    public void Extract(string TwisonOutput)
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
        List<string> passageNameList = new List<string>();
        string startPassageName = string.Empty;
        foreach (twisonPassage passage in arrayOfRawPassages)
        {
            string passageName = passage.name;
            if(passageNameList.Contains(passageName))
            {
                Debug.LogWarning("Duplicate Passage Name: " + passageName);
                continue;
            }
            else
                passageNameList.Add(passageName);
            int passageID = passage.pid;
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

        for (int i = 1; i <= numberOfPassages; i++)//TODO investigate array outside range of collection
        {
            string passageName = listOfPassageMetadata[i - 1].Name;//id mismatch, starts from 1 and not 0
            try
            {
                FormatExtractor extractor = new HarloweExtractor(rawPassageTexts[i]);
                string[] passageText = extractor.Extract();
                File.AppendAllText(path, $"public IEnumerator<VinePassageOutput> Passage{i}()//{passageName}\n" + "{\n");
                foreach (string line in passageText)
                    File.AppendAllText(path, line + "\n");
                File.AppendAllText(path, "\n}\n");
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
        bool Extract = GUILayout.Button(nameof(Extract));
        bool Clear = GUILayout.Button(nameof(Clear));
        TwisonExtractor extractor = (TwisonExtractor)target;
        EditorGUILayout.LabelField("Paste Twison Output Here");
        DrawDefaultInspector();
        if (Extract)
            extractor.Extract(extractor.TwisonOutput);
        else if (Clear)
            extractor.TwisonOutput = string.Empty;
    }
}
public abstract class FormatExtractor
{
    public FormatExtractor(string input) { }
    public abstract string[] Extract();
}
#endif