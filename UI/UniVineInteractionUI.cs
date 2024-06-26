using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vine;
public class UniVineInteractionUI : MonoBehaviour, VineInteraction
{
    public TMP_Text Header,Body;
    public Button ChoiceButtonPrefab;
    public UniVinePortraitFrame PortraitFramePrefab;
    public UniVineTimerUI TimerPrefab;
    public RectTransform WindowFrame, ButtonMount, PortraitAnchor, TimerMount;
    Image _CharacterDisplay;
    float _buttonSizeY;
    public void ProcessOutput(VinePassageOutput output)
    {
        if (output is VineHeaderOutput h)
            SetHeader(h);
        else if (output is VineDelayActionOutput delayAction)
            SetTimer(delayAction);
        else if (output is VineClickActionOutput clickAction)
            SetClickAction(clickAction);
        else if (output is VineLinkOutput link)
            SetLink(link);
        else
            throw new System.Exception(output.GetType().ToString() + "unsupported for this interaction");
    }
    public void Initialise(VinePassageMetadata metadata, Sprite playerSprite)
    {
        UniVinePortraitFrame frame = Instantiate(PortraitFramePrefab, PortraitAnchor);
        _CharacterDisplay = frame.CharacterDisplay;
        _CharacterDisplay.sprite = playerSprite;
        _buttonSizeY = ChoiceButtonPrefab.GetComponent<RectTransform>().sizeDelta.y;
    }
    void OnChoiceMade(string passageName)
    {
        UniVinePlayer.Instance.Loader.LoadPassage(passageName);
        StopAllCoroutines();
        Destroy(gameObject);
    }
    void SetHeader(VineHeaderOutput h)
    {
        Header.text = h.Header;
        Body.text = h.Body;
    }
    void SpawnChoiceButton(string textClick, UnityEngine.Events.UnityAction listener)
    {
        Button go = Instantiate(ChoiceButtonPrefab, ButtonMount);
        RectTransform windowFrame = (RectTransform)transform;//TODO test RectTransform casting
        Vector2 sizeDelta = WindowFrame.sizeDelta;
        WindowFrame.sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y + _buttonSizeY);
        var t = go.GetComponentInChildren<TMP_Text>();
        t.text = textClick;
        go.onClick.AddListener(listener);
        ButtonMount.ShuffleChildren();
    }
    void SetLink(VineLinkOutput h)
    {
        SpawnChoiceButton(h.TextClick, () => OnChoiceMade(h.PassageName));
    }
    void SetClickAction(VineClickActionOutput link)
    {
        SpawnChoiceButton(link.TextClick, link.ActionBlock);
    }
    void SetTimer(VineDelayActionOutput dlink)
    {
        StartCoroutine(Timer(dlink));//TODO show timer
    }
    IEnumerator Timer(VineDelayActionOutput delayOutput)
    {
        UniVineTimerUI timer = Instantiate(TimerPrefab, TimerMount);
        float timeLeft = delayOutput.Delay;
        timer.SetDuration(timeLeft);
        while(timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            timer.UpdateTimer(timeLeft);
            yield return new WaitForEndOfFrame();
        }
        delayOutput.ActionBlock();
    }
}