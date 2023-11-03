using System.Collections;
using UnityEngine;
using Vine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
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
        else if (output is VineDelayLinkOutput dlink)
            SetTimer(dlink);
        else if (output is VineLinkOutput link)
            SetLink(link);
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
    void SetLink(VineLinkOutput h)
    {
        Button go = Instantiate(ChoiceButtonPrefab, ButtonMount);
        WindowFrame.sizeDelta = new Vector2(WindowFrame.sizeDelta.x, WindowFrame.sizeDelta.y + _buttonSizeY);
        var t = go.GetComponentInChildren<TMP_Text>();
        t.text = h.TextClick;
        go.onClick.AddListener(()=>OnChoiceMade(h.PassageName));
        ShuffleChildren(ButtonMount);
    }
    void ShuffleChildren(Transform parent)//TODO make this a utility function
    {
        int childCount = parent.childCount;
        List<int> randomBagOfKeys = new List<int>();
        for (int i = 0; i < childCount; i++)
            randomBagOfKeys.Add(i);
        for (int i = 0; i < randomBagOfKeys.Count; i++)
        {
            int randomIndex = Random.Range(0, randomBagOfKeys.Count);
            int randomChildKey = randomBagOfKeys[randomIndex];
            Transform randomChild = parent.GetChild(randomChildKey);
            randomBagOfKeys.RemoveAt(randomIndex);
            int roll = Random.Range(0, 2);
            if (roll == 0)
                randomChild.SetAsFirstSibling();
            else
                randomChild.SetAsLastSibling();
        }
    }
    void SetTimer(VineDelayLinkOutput dlink)
    {
        StartCoroutine(Timer(dlink));//TODO show timer
    }
    IEnumerator Timer(VineDelayLinkOutput dlink)
    {
        UniVineTimerUI timer = Instantiate(TimerPrefab, TimerMount);
        float timeLeft = dlink.Delay;
        timer.SetDuration(timeLeft);
        while(timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            timer.UpdateTimer(timeLeft);
            yield return new WaitForEndOfFrame();
        }
        OnChoiceMade(dlink.PassageName);
    }
}