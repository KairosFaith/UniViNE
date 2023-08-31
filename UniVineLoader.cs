using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEngine.EventSystems;
using Vine;
using TMPro;
public class UniVineLoader : MonoBehaviour, IPointerDownHandler
{
    const string Interaction = "Interaction";
    public UniVinePlayer Player;
    public float LetterRate = .1f;
    VineStory _Story;
    bool _Pressed = false;
    //Data to Save
    string _CurrentPassage, _JsonSave;
    public string CurrentPlayerCharacter;
    void Start()
    {
        StartStory("PrologueTestStory");
    }
    void StartStory(string StoryID)
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
        var pdata = _Story.FetchNextPassage(passageName, out string n);
        _CurrentPassage = passageName;
        _JsonSave = _Story.PackVariables();
        MethodInfo method = typeof(PrologueTestStory).GetMethod(n);
        var passage = (IEnumerable<VinePassageOutput>)method.Invoke(_Story, null);
        if (pdata.Name.Contains(Interaction))//TODO check passage type
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
                _Pressed = false;
                TMP_Text textBox = Player.OutputLine(line, this);
                int stringLength = line.Text.Length;
                for (int i = 0;i< stringLength; i++)
                {
                    textBox.maxVisibleCharacters = i;
                    yield return new WaitForSeconds(LetterRate);
                    if (_Pressed)
                    {
                        textBox.maxVisibleCharacters = stringLength;
                        break;
                    }
                }
                _Pressed = false;
                while (!_Pressed)
                    yield return new WaitForEndOfFrame();
                Player.ClearPrevObject();
                _Pressed = false;
            }
            else if (output is VineMarkedOutput mark)
                Player.ProcessMarker(mark, this);
            else if (output is VineLinkOutput link)
                LoadPassage(link.PassageName);
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
        _Pressed = true;
    }
}