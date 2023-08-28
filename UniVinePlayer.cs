using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vine;
public class UniVinePlayer : MonoBehaviour
{
    public TMP_Text TextBoxPrefab;
    public UniVineInteractionUI InteractionPrefab;
    public AudioSource Source;
    public Image BackgroundDisplay;
    Dictionary<string, Sprite> BackgroundSpritesBank = new Dictionary<string, Sprite>();
    Dictionary<(string, VineCharacterEmotion), Sprite> CharacterSpriteBank = new Dictionary<(string, VineCharacterEmotion), Sprite>();
    Dictionary<string, string> CharacterToSpriteLink = new Dictionary<string, string>();
    void Awake()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Backgrounds");
        foreach (var sprite in sprites)
            BackgroundSpritesBank.Add(sprite.name, sprite);
    }
    public TMP_Text OutputLine(VineLineOutput line)
    {
        var t = Instantiate(TextBoxPrefab, transform);
        t.maxVisibleCharacters = 0;
        return t;
    }
    public void ProcessMarker(VineMarkedOutput mark)
    {
        switch (mark.MarkType)
        {
            case VineMarkType.Music:
                //fade?
                Source.Stop();
                //change clip
                Source.Play();
                break;
            case VineMarkType.Background:
                BackgroundDisplay.sprite = BackgroundSpritesBank[mark.Text[0]];
                break;
            case VineMarkType.SetPlayerCharacter:
                break;
            case VineMarkType.SetCharacterSprite:
                LoadSprites(mark);
                break;
        }
    }
    void LoadSprites(VineMarkedOutput mark)
    {
        string[] args = mark.Text;
        string spriteID = args[1];
        CharacterToSpriteLink[args[0]] = args[1];
        foreach (VineCharacterEmotion emotion in Enum.GetValues(typeof(VineCharacterEmotion)))
        {
            string spriteFileName = spriteID + emotion;
            Sprite s = Resources.Load<Sprite>("CharacterSprites/" + spriteFileName);
            CharacterSpriteBank[(spriteID, emotion)] = s;
        }
    }
    public UniVineInteractionUI PlayInteraction(VinePassageMetadata metadata)
    {
        return Instantiate(InteractionPrefab,transform);
    }
}