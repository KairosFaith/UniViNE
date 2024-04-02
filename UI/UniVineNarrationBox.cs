using System.Collections;
using TMPro;
using UnityEngine;
using Vine;
public class UniVineNarrationBox : IUniVineTextBox
{
    //TODO use content size fitter instead
    //public RectTransform[] AdjustToText;
    //public override void InitiateBox(VineLineOutput line)
    //{
    //    OnTextUpdate = () =>
    //    {
    //        foreach(RectTransform rt in AdjustToText)
    //        {
    //            Vector2 newSize = rt.sizeDelta;
    //            newSize.y = MainTextBox.preferredHeight;
    //            rt.sizeDelta = newSize;
    //        }
    //    };
    //    base.InitiateBox(line);
    //}
    protected override IEnumerator BoxRoutine(float duration, float letterRate)
    {
        for (float t = 0; t <= duration; t += Time.deltaTime)
        {
            ScaleBox(t / duration);
            yield return new WaitForEndOfFrame();
        }
        ScaleBox(1);//just in case
        OnBoxOpen?.Invoke();
        TMP_TextInfo textInfo = MainTextBox.textInfo;
        int textLength = textInfo.characterCount;
        //TODO clean up this coroutine
        for (int i = 0; i <= textLength; i++)
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
        MainTextBox.maxVisibleCharacters = textLength;
        ShowContinuePrompt();
        yield return new WaitUntil(() => _Pressed);
        _Pressed = false;
        OnBoxClose?.Invoke();
        for (float t = duration; t >= 0; t -= Time.deltaTime)
        {
            ScaleBox(t / duration);
            yield return new WaitForEndOfFrame();
        }
        Destroy(gameObject);
        UniVinePlayer.Instance.ContinuePassage();
    }
}