using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace Vine
{
public enum VineCharacterEmotion
{
    Default,
    Smile,
    Angry,
    Sad,
    Scream,
}
    public interface VinePlayer
    {
        public VineLoader Loader { get; set; }
        public void OutputLine(VineLineOutput line);
        public void ShowTitle(VineHeaderOutput header);
        public VineInteraction PlayInteraction(VinePassageMetadata metadata);
        public void SendMessage(string methodName, object value = null, SendMessageOptions options = SendMessageOptions.RequireReceiver);
    }
    public interface VineInteraction
    {
        public void ProcessOutput(VinePassageOutput output);
    }
    public class VineLoader
    {
        const string Interaction = "Interaction";
        public VinePlayer StoryPlayer;
        public VineLoader(VinePlayer storyPlayer)
        {
            StoryPlayer = storyPlayer;
        }
        VineStory _Story;
        //Data to Save
        string _CurrentPassageName, _JsonSave;
        public Dictionary<string, string> CharacterToSpriteLink = new Dictionary<string, string>();
        public string CurrentPlayerCharacter;
        IEnumerator<VinePassageOutput> _CurrentPassage;
        public void StartStory(string StoryID)
        {
            _Story = Activator.CreateInstance(Type.GetType(StoryID)) as VineStory;
            LoadPassage(_Story.StartPassage);
        }
        void SaveStory()
        {

        }
        void ResumeStory(string StoryID)
        {

        }
        public void LoadPassage(string passageName)
        {
            var pdata = _Story.FetchPassage(passageName, out string n);
            _CurrentPassageName = passageName;
            _JsonSave = _Story.PackVariables();
            MethodInfo method = typeof(Prologue).GetMethod(n);
            var passage = (IEnumerator<VinePassageOutput>)method.Invoke(_Story, null);
            if (pdata.Name.Contains(Interaction))//TODO check passage type
                InteractionRoutine(passage, pdata);
            else
            {
                _CurrentPassage = passage;
                NextLineInPassage();
            }
        }
        public void NextLineInPassage()
        {
            if (_CurrentPassage.MoveNext())
            {
                VinePassageOutput output = _CurrentPassage.Current;
                if (output is VineLineOutput line)
                    StoryPlayer.OutputLine(line);
                else if (output is VineHeaderOutput header)
                    StoryPlayer.ShowTitle(header);
                else if (output is UniVineMarkedOutput mark)
                {
                    StoryPlayer.SendMessage(mark.MethodName, mark.Value);
                    NextLineInPassage();
                }
                else if (output is VineLinkOutput link)
                    LoadPassage(link.PassageName);
                else
                    Debug.LogError("Unknown output type");
            }
            else
            {
                _CurrentPassage = null;//TODO????
                Debug.Log("End of passage");
            }
        }
        void InteractionRoutine(IEnumerator<VinePassageOutput> passage, VinePassageMetadata metadata)
        {
            VineInteraction UI = UniVinePlayer.Instance.PlayInteraction(metadata);
            while (passage.MoveNext())
                UI.ProcessOutput(passage.Current);
        }
    }
    public abstract class VineStory
{
    public abstract string JSON_Metadata { get; }
    public string StoryName, StartPassage;
    public Dictionary<string, VinePassageMetadata> Passages = new Dictionary<string, VinePassageMetadata>();
    public Dictionary<string, VineVar> Variables = new Dictionary<string, VineVar>();
    public VineStory()
    {
        VineStoryMetadata s = JsonUtility.FromJson<VineStoryMetadata>(JSON_Metadata);
        StartPassage = s.StartPassage;
        StoryName = s.StoryName;
        VinePassageMetadata[] p = s.Data;
        foreach (var metadataItem in p)
            Passages.Add(metadataItem.Name, metadataItem);
    }
    public VinePassageMetadata FetchPassage(string passageName, out string functionName)
    {
        if(Passages.TryGetValue(passageName, out VinePassageMetadata metadata))
        {
            functionName = $"Passage{metadata.ID}";
            return metadata;
        }
        throw new Exception($"Passage {passageName} not found");
    }
        public string PackVariables()
        {
            return JsonUtility.ToJson(Variables);
        }
}
    public struct VineVar
    {
        object data;
        public VineVar(object setAs)
        {
            data = setAs;
        }
        public static implicit operator VineVar(int value)
        {
            return new VineVar(value);
        }
        public static implicit operator VineVar(string value)
        {
            return new VineVar(value);
        }
        public static implicit operator VineVar(float value)
        {
            return new VineVar(value);
        }
        public static implicit operator VineVar(bool value)
        {
            return new VineVar(value);
        }
        public static implicit operator string(VineVar v)
        {
            Type t = v.data.GetType();
            if (t == typeof(int))
            {
                int i = (int)v.data;
                return i.ToString();
            }
            else if (t == typeof(float))
            {
                float f = (float)v.data;
                return f.ToString();
            }
            else if (t == typeof(bool))
            {
                bool b = (bool)v.data;
                return b.ToString();
            }
            else if (t == typeof(string))
                return (string)v.data;
            else
                throw new Exception("Variable is of invalid type");
        }
        public static VineVar operator +(VineVar v, int i)
        {
            Type t = v.data.GetType();
            if (t == typeof(int))
                return (int)v.data + i;
            else if (t == typeof(float))
                return (float)v.data + i;
                return (string)v + i;
        }
        public static VineVar operator +(VineVar v, float f)
        {
            Type t = v.data.GetType();
            if (t == typeof(float)|t==typeof(int))
                return (float)v.data + f;
                return (string)v + f;
        }
    }
[Serializable]
public class VineStoryMetadata
{
    public string StoryName;
    public string StartPassage;
    public VinePassageMetadata[] Data;
}
[Serializable]
public class VinePassageMetadata
{
    public string Name;
    public int ID;
    public string[] Tags;
}
public abstract class VinePassageOutput { }
public class VineLineOutput : VinePassageOutput
{
    public string Character, Text;
    public VineCharacterEmotion Emotion;
    public VineLineOutput(string character, string text)
    {
        Character = character;
        Text = text;
    }
    public VineLineOutput(string character, VineCharacterEmotion emotion, string text)
    {
        Character = character;
        Text = text;
        Emotion = emotion;
    }
}
[Serializable]
public class VineLinkOutput : VinePassageOutput
{
    public string TextClick, PassageName;
    public VineLinkOutput(string passageName)
    {
        TextClick = PassageName = passageName;
    }
    public VineLinkOutput(string textClick,string passageName)
    {
        TextClick = textClick;
        PassageName = passageName;
    }
    public VineLinkOutput(){ } //nonsense
}
public class VineHeaderOutput : VinePassageOutput
{
    public string Header, Body;
    public VineHeaderOutput(string header, string body)
    {
        Header = header;
        Body = body;
    }
}
public class VineDelayLinkOutput : VineLinkOutput
{
    public float Delay;
    public VineDelayLinkOutput(float delay, string passageName)
    {
        Delay = delay;
        PassageName = passageName;
    }
    public VineDelayLinkOutput() { }//C# nonsense 
}
}