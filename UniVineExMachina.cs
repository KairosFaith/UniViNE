using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Vine;
using System;
using System.Collections.Generic;
public abstract class IUniVineTextBox : MonoBehaviour, IPointerDownHandler
{
    public TMP_Text MainTextBox;
    public List<Transform> FramesToShrink;
    public RectTransform ContinuePromptPrefab;
    protected bool _Pressed;
    protected Action OnBoxOpen, OnBoxClose, OnTextUpdate;
    public virtual void InitiateBox(VineLineOutput line)
    {
        MainTextBox.text = line.Text;
        MainTextBox.maxVisibleCharacters = 0;
        UniVinePlayer instance = UniVinePlayer.Instance;
        StartCoroutine(BoxRoutine(instance.BoxOpenCloseRate, instance.LetterRate));
    }
    protected void ScaleBox(float t)
    {
        foreach (Transform frame in FramesToShrink)
            frame.localScale = Vector3.one * t;
    }
    protected void ShowContinuePrompt()
    {
        Instantiate(ContinuePromptPrefab, MainTextBox.rectTransform);
    }
    protected virtual IEnumerator BoxRoutine(float duration, float letterRate)
    {
        for (float t = 0; t <= duration; t += Time.deltaTime)
        {
            ScaleBox(t / duration);
            yield return new WaitForEndOfFrame();
        }
        ScaleBox(1);//just in case
        OnBoxOpen?.Invoke();
        int pageCount = MainTextBox.textInfo.pageCount;
        for(int pageNumber = 1; pageNumber <= pageCount;)
        {
            TMP_PageInfo currentPageInfo = MainTextBox.textInfo.pageInfo[pageNumber - 1];
            int visibleCharactersToFillPage = currentPageInfo.lastCharacterIndex + 1;
            //TODO clean up this coroutine
            for (int i = currentPageInfo.firstCharacterIndex + 1; i <= visibleCharactersToFillPage; i++)
            {
                MainTextBox.maxVisibleCharacters = i;
                yield return new WaitForSeconds(letterRate);
                if (_Pressed)
                {
                    _Pressed = false;
                    break;
                }
                OnTextUpdate?.Invoke();
            }
            MainTextBox.maxVisibleCharacters = visibleCharactersToFillPage;
            ShowContinuePrompt();
            yield return new WaitUntil(() => _Pressed);
            _Pressed = false;
            pageNumber++;
            MainTextBox.pageToDisplay = pageNumber;
        }
        MainTextBox.maxVisibleCharacters = MainTextBox.textInfo.characterCount;
        OnBoxClose?.Invoke();
        for (float t = duration; t >= 0; t -= Time.deltaTime)
        {
            ScaleBox(t / duration);
            yield return new WaitForEndOfFrame();
        }
        Destroy(gameObject);
        UniVinePlayer.Instance.ContinuePassage();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        _Pressed = true;
    }
}