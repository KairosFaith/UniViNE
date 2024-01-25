using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace Vine
{
    public enum VineCharacterEmotion
    {
        neutral,
        smile,
        angry,
        sad,
        scream,
    }
    public interface VinePlayer
    {
        public VineLoader Loader { get; set; }
        public Dictionary<string, Action> SpecialMarks { get; set; }
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
        Type StoryClass;
        //Data to Save
        string _CurrentPassageName, _JsonSave;
        public Dictionary<string, string> CharacterToSpriteLink = new Dictionary<string, string>();
        public string CurrentPlayerCharacter;
        IEnumerator<VinePassageOutput> _CurrentPassage;
        public void StartStory(string StoryID)
        {
            StoryClass = Type.GetType(StoryID.RemoveIllegalClassCharacters());
            _Story = Activator.CreateInstance(StoryClass) as VineStory;
            LoadPassage(_Story.StartPassage);
        }
        public void LoadPassage(string passageName)
        {
            var pdata = _Story.FetchPassage(passageName, out string n);
            _CurrentPassageName = passageName;
            _JsonSave = _Story.PackVariables();
            MethodInfo method = StoryClass.GetMethod(n);
            var passage = (IEnumerator<VinePassageOutput>)method.Invoke(_Story, null);
            _Story.HistoryPassages.Add(passageName);
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
                    if (StoryPlayer.SpecialMarks.TryGetValue(mark.MethodName, out Action func))
                        func();
                    else
                        StoryPlayer.SendMessage(mark.MethodName.Trim(), mark.Value);
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
        public abstract string StoryName { get; }
        public abstract string StartPassage { get; }
        public abstract Dictionary<string, VinePassageMetadata> Passages { get; }
        public List<string> HistoryPassages = new List<string>();
        public Dictionary<string, VineVar> StoryVariables = new Dictionary<string, VineVar>();
        public Dictionary<string, VineVar> Set => StoryVariables;//Store Variables
        public VineVar Get(string variableName)
        {
            if (Set.TryGetValue(variableName, out VineVar v))
                return v;
            else
            {
                VineVar n = new VineVar();
                Set.Add(variableName, n);
                return n;
            }
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
            return JsonUtility.ToJson(StoryVariables);
        }
    }
    public struct VineVar
    {
        public object data;
        public VineVar(object setAs)
        {
            data = setAs;
        }
        public static implicit operator VineVar(int value)
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
        public static implicit operator VineVar(string value)
        {
            if(int.TryParse(value, out int i))
                return i;
            else if(float.TryParse(value, out float f))
                return f;
            else if(bool.TryParse(value, out bool b))
                return b;
            return new VineVar(value);
        }
        public override readonly string ToString()//TODO check if readonly is correct
        {
            if (data == null)
                return 0.ToString();//return 0 for printing
            Type t = data.GetType();
            if (t == typeof(int))
            {
                int i = (int)data;
                return i.ToString();
            }
            else if (t == typeof(float))
            {
                float f = (float)data;
                return f.ToString();
            }
            else if (t == typeof(bool))
            {
                bool b = (bool)data;
                return b.ToString();
            }
            else if (t == typeof(string))
                return (string)data;
            return data.ToString();
        }
        public static VineVar operator +(VineVar v, string s)
        {
            object data = v.data;
            if(data ==null)
                return s;
            return (string)data + s;
        }
        public static VineVar operator +(VineVar v, int i)
        {
            if(v.data==null)
                return i;
            Type t = v.data.GetType();
            if (t == typeof(int))
                return (int)v.data + i;
            else if (t == typeof(float))
                return (float)v.data + i;
            else if (t == typeof(string))
                return (string)v.data + i;
            return i;
        }
        public static VineVar operator +(VineVar v, float f)
        {
            if (v.data == null)
                return f;
            Type t = v.data.GetType();
            if (t == typeof(float)|t==typeof(int))
                return (float)v.data + f;
            return (string)v.data + f;
        }
        public static bool operator > (VineVar v, float f)
        {
            Type t = v.data.GetType();
            if (t == typeof(int))
                return (int)v.data > f;
            else if (t == typeof(float))
                return (float)v.data > f;
            else
                throw new Exception("Variable is of invalid type");
        }
        public static bool operator < (VineVar v, float f)
        {
            Type t = v.data.GetType();
            if (t == typeof(int))
                return (int)v.data < f;
            else if (t == typeof(float))
                return (float)v.data < f;
            else
                throw new Exception("Variable is of invalid type");
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
        public VinePassageMetadata(string name, int id, params string[] tags)
        {
            Name = name;
            ID = id;
            Tags = tags;
        }
        public VinePassageMetadata(string name, int id)
        {
            Name = name;
            ID = id;
        }
        public VinePassageMetadata() { }//C# nonsense
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
    public class UniVineMarkedOutput : VinePassageOutput
    {
        public string MethodName;
        public object Value;
        public UniVineMarkedOutput(string methodName, object value = null)
        {
            MethodName = methodName;
            Value = value;
        }
        public UniVineMarkedOutput(string methodName, params object[] value)
        {
            MethodName = methodName;
            Value = value;
        }
    }
}