using UnityEngine;
using UnityEngine.UI;
public class UniVinePortraitFrame : MonoBehaviour
{
    public Image CharacterDisplay;
    public void SetCharacterSprite(Sprite sprite, bool isPlayer)
    {
        CharacterDisplay.sprite = sprite;
        if(isPlayer)
            CharacterDisplay.transform.localScale = new Vector3(-1, 1, 1);
    }
}