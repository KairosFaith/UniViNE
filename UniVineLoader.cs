using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Vine;
public class UniVineLoader : MonoBehaviour
{
<<<<<<< HEAD
    VineStory Story;
    public UniVinePlayer Player;
    public Action LineCallback;
    //Current Data to save
    string CurrentPassage, JsonSave;
    public string CurrentPlayerCharacter;
    public Dictionary<string, string> CharacterToSpriteLink = new Dictionary<string, string>();
    List<string> _HistoryPassage = new List<string>();
    void Start()//for testing only, To remove
=======
    const string Interaction = "Interaction";
    public UniVinePlayer Player;
    public float LetterRate = .1f;
    VineStory _Story;
    bool _Pressed = false;
    //Data to Save
    string _CurrentPassage, _JsonSave;
    public string CurrentPlayerCharacter;
    void Start()
>>>>>>> 3ac408fac85cc3d344a5afcca5c04fb29212791a
    {
        StartStory("PrologueTestStory");
    }
    void StartStory(string StoryID)
    {
<<<<<<< HEAD
        Story = Activator.CreateInstance(Type.GetType(StoryID)) as VineStory;
        LoadPassage(Story.StartPassage);
=======
        _Story = Activator.CreateInstance(Type.GetType(StoryID)) as VineStory;
        LoadPassage(_Story.StartPassage);
    }
    void SaveStory()
    {

    }
    void ResumeStory(string StoryID)
    {

>>>>>>> 3ac408fac85cc3d344a5afcca5c04fb29212791a
    }
    void SaveStory()
    {

    }
    void ResumeStory(string StoryID)
    {

    }
    public void LoadPassage(string passageName)
    {
<<<<<<< HEAD
        VinePassageMetadata pdata = Story.FetchNextPassage(passageName, out string n);
        JsonSave = Story.PackVariables();
        MethodInfo method = typeof(PrologueTestStory).GetMethod(n);
        var passage = (IEnumerable<VinePassageOutput>)method.Invoke(Story, null);
        CurrentPassage = passageName;
        _HistoryPassage.Add(passageName);
        if (passageName.Contains("Interaction"))//TODO check passage type
=======
        var pdata = _Story.FetchNextPassage(passageName, out string n);
        _CurrentPassage = passageName;
        _JsonSave = _Story.PackVariables();
        MethodInfo method = typeof(PrologueTestStory).GetMethod(n);
        var passage = (IEnumerable<VinePassageOutput>)method.Invoke(_Story, null);
        if (pdata.Name.Contains(Interaction))//TODO check passage type
>>>>>>> 3ac408fac85cc3d344a5afcca5c04fb29212791a
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
<<<<<<< HEAD
                bool _Pressed = false;
                LineCallback = () => _Pressed = true;
                //show text
                Player.OutputLine(line, this);
                //wait
                yield return new WaitUntil(() => _Pressed);
            }
            else if (output is VineMarkedOutput mark)
                ProcessMarker(mark);
=======
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
>>>>>>> 3ac408fac85cc3d344a5afcca5c04fb29212791a
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
<<<<<<< HEAD
=======
    public void OnPointerDown(PointerEventData eventData)
    {
        _Pressed = true;
    }
>>>>>>> 3ac408fac85cc3d344a5afcca5c04fb29212791a
}