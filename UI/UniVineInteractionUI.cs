using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vine;
using TMPro;
using UnityEngine.UI;

public class UniVineInteractionUI : MonoBehaviour
{
    UniVineLoader Loader;
    public TMP_Text Header,Body;
    public Button ChoiceButtonPrefab;
    public Transform ButtonPanel;
    void OnChoiceMade(string passageName)
    {
        Loader.LoadPassage(passageName);
        StopAllCoroutines();
        Destroy(gameObject);
    }
    public void SetHeader(VineHeaderOutput h,UniVineLoader loader)
    {
        Loader = loader;
        Header.text = h.Header;
        Body.text = h.Body;
    }
    public void SetLink(VineLinkOutput h)
    {
        Button go = Instantiate(ChoiceButtonPrefab, ButtonPanel);
        TMP_Text t = go.GetComponentInChildren<TMP_Text>();
        t.text = h.TextClick;
        go.onClick.AddListener(()=>OnChoiceMade(h.PassageName));
    }
    public void SetTimer(VineDelayLinkOutput dlink)
    {
        StartCoroutine(Timer(dlink));
    }
    IEnumerator Timer(VineDelayLinkOutput dlink)
    {
        yield return new WaitForSeconds(dlink.Delay);
        OnChoiceMade(dlink.PassageName);
    }
}