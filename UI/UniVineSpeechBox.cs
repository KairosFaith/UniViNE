using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vine;
public class UniVineSpeechBox : MonoBehaviour
{
    public Image CharacterDisplay;
    public TMP_Text Header, Body;
    public TMP_Text SetSpeechBox(VineLineOutput line, Sprite characterSprite)
    {
        Header.text = line.Character;
        CharacterDisplay.sprite = characterSprite;
        Body.text = line.Text;
        Body.maxVisibleCharacters = 0;
        return Body;
    }
}