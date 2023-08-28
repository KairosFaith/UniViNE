using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEditor.U2D.Sprites;
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
public enum VineMarkType
{
    SetPlayerCharacter,
    SetCharacterSprite,
    Invoke,//Invoke Unity Function
    Background,
    Music,
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
    public VinePassageMetadata FetchNextPassage(string passageName, out string functionName)
    {
        if(Passages.TryGetValue(passageName, out VinePassageMetadata metadata))
        {
            functionName = $"Passage{metadata.ID}";
            return metadata;
        }
        functionName = null;
        return null;
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
public class VineMarkedOutput : VinePassageOutput
{
    public VineMarkType MarkType;
    public string[] Text;
    //public VineMarkedOutput(string characterName)
    //{
    //    MarkType = VineMarkType.PlayerCharacter;
    //    Text = characterName;
    //}
    public VineMarkedOutput(VineMarkType mark, params string[] text)
    {
        MarkType = mark;
        Text = text;
    }
}
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
        //TextClick = PassageName = passageName;
    }
    public VineDelayLinkOutput() { }//nonsense 
}
}