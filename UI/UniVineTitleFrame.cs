using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Vine;
public class UniVineTitleFrame : MonoBehaviour, IPointerDownHandler
{
    public TMP_Text Header, Body;
    public void SetHeader(VineHeaderOutput h)
    {
        Header.text = h.Header;
        Body.text = h.Body;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        //TODO fade out?
        Destroy(gameObject);
        UniVinePlayer.Instance.Loader.NextLineInPassage();
    }
}