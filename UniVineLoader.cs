using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEngine.EventSystems;
using Vine;
using TMPro;
using UnityEngine.Networking.Types;
using System;

public class UniVineLoader : MonoBehaviour, IPointerDownHandler
{
    public UniVinePlayer Player;
    VineStory Story;
    string CurrentPassage, JsonSave;
    bool Pressed = false;
    //Current Data to save
    public string CurrentPlayerCharacter;
    public Dictionary<string, string> CharacterToSpriteLink = new Dictionary<string, string>();
    List<string> HistoryPassage = new List<string>();
    void Start()
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
        HistoryPassage.Add(passageName);
        if (passageName.Contains("Interaction"))//TODO check passage type
            InteractionRoutine(passage,pdata);
        else
            StartCoroutine(PassageRoutine(passage));
    }
    IEnumerator PassageRoutine(IEnumerable<VinePassageOutput> passage)
    {
        TMP_Text textBox = null;
        foreach (VinePassageOutput output in passage)
        {
            if (output is VineLineOutput line)
            {
                Pressed = false;
                textBox = Player.OutputLine(line,this);
                string show = textBox.text = line.Text;
                int stringLength = show.Length;
                for (int i = 0;i< stringLength; i++)
                {
                    textBox.maxVisibleCharacters = i;
                    yield return new WaitForSeconds(.1f);
                    if (Pressed)
                    {
                        textBox.maxVisibleCharacters = stringLength;
                        Pressed = false;
                        break;
                    }
                }
                while (!Pressed)
                    yield return new WaitForEndOfFrame();
                Pressed = false;
                Destroy(textBox.gameObject);
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
    public void OnPointerDown(PointerEventData eventData)
    {
        Pressed = true;
    }
}