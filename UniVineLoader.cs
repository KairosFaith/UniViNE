using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEngine.EventSystems;
using Vine;
using TMPro;
public class UniVineLoader : MonoBehaviour, IPointerDownHandler
{
    public UniVinePlayer Player;
    VineStory Story;
    string CurrentPassage, JsonSave;
    bool Pressed = false;
    void Start()
    {
        Story = new PrologueTestStory();
        LoadPassage(Story.StartPassage);
    }
    public void LoadPassage(string passageName)
    {
        var pdata = Story.FetchNextPassage(passageName, out string n);
        CurrentPassage = passageName;
        JsonSave = Story.PackVariables();
        MethodInfo method = typeof(PrologueTestStory).GetMethod(n);
        var passage = (IEnumerable<VinePassageOutput>)method.Invoke(Story, null);
        if (pdata.Name.Contains("Interaction"))//TODO check passage type
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
                textBox = Player.OutputLine(line);
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
                Player.ProcessMarker(mark);
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
        Pressed = true;
    }
}