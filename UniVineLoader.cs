using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Vine;
public class UniVineLoader : MonoBehaviour
{
    VineStory Story;
    public UniVinePlayer Player;
    public Action LineCallback;
    //Current Data to save
    string CurrentPassage, JsonSave;
    public string CurrentPlayerCharacter;
    public Dictionary<string, string> CharacterToSpriteLink = new Dictionary<string, string>();
    List<string> _HistoryPassage = new List<string>();
    void Start()//for testing only, To remove
    {
        StartStory("PrologueTestStory");
    }
    void StartStory(string StoryID)
    {
        Story = Activator.CreateInstance(Type.GetType(StoryID)) as VineStory;
        LoadPassage(Story.StartPassage);
    }
    void SaveStory()
    {

    }
    void ResumeStory(string StoryID)
    {

    }
    public void LoadPassage(string passageName)
    {
        VinePassageMetadata pdata = Story.FetchNextPassage(passageName, out string n);
        JsonSave = Story.PackVariables();
        MethodInfo method = typeof(PrologueTestStory).GetMethod(n);
        var passage = (IEnumerable<VinePassageOutput>)method.Invoke(Story, null);
        CurrentPassage = passageName;
        _HistoryPassage.Add(passageName);
        if (passageName.Contains("Interaction"))//TODO check passage type
            InteractionRoutine(passage,pdata);
        else
            StartCoroutine(PassageRoutine(passage));
    }
    IEnumerator PassageRoutine(IEnumerable<VinePassageOutput> passage)
    {
        foreach (VinePassageOutput output in passage)
        {
            if (output is VineLineOutput line)
            {
                bool _Pressed = false;
                LineCallback = () => _Pressed = true;
                //show text
                Player.OutputLine(line, this);
                //wait
                yield return new WaitUntil(() => _Pressed);
            }
            else if (output is VineMarkedOutput mark)
                ProcessMarker(mark);
            else if (output is VineLinkOutput link)
                LoadPassage(link.PassageName);
        }
    }
    void ProcessMarker(VineMarkedOutput mark)
    {
        switch (mark.MarkType)
        {
            case VineMarkType.Music:
                Player.SetMusic(mark.Text[0]);
                break;
            case VineMarkType.Background:
                Player.SetBackground(mark.Text[0]);
                break;
            case VineMarkType.SetPlayerCharacter:
                CurrentPlayerCharacter = mark.Text[0];
                break;
            case VineMarkType.SetCharacterSprite:
                Player.LoadCharacterSprites(mark, this);
                break;
        }
    }
    void InteractionRoutine(IEnumerable<VinePassageOutput> passage, VinePassageMetadata metadata)
    {
        UniVineInteractionUI UI = Player.PlayInteraction(metadata);
        foreach (VinePassageOutput output in passage)
            if (output is VineHeaderOutput h)
                UI.SetHeader(h,this);
            else if (output is VineLinkOutput link)
                UI.SetLink(link);
            else if (output is VineDelayLinkOutput dlink)
                UI.SetTimer(dlink);
    }
}