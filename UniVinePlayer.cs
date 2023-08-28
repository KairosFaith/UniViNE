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
    //Data To Save
    Dictionary<string, string> CharacterToSpriteLink = new Dictionary<string, string>();
    string CurrentPlayerCharacter;
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
                string id = mark.Text[0];
                if (BackgroundSpritesBank.TryGetValue(id, out Sprite bg))
                    BackgroundDisplay.sprite = bg;
                else
                    BackgroundDisplay.sprite = LoadBackground(id);
                break;
            case VineMarkType.SetPlayerCharacter:
                CurrentPlayerCharacter = mark.Text[0];
                break;
            case VineMarkType.SetCharacterSprite:
                LoadCharacterSprites(mark);
                break;
        }
    }
    void LoadCharacterSprites(VineMarkedOutput mark)
    {
        string[] args = mark.Text;
        string spriteID = args[1];
        CharacterToSpriteLink[args[0]] = args[1];
        if (CharacterSpriteBank.TryGetValue((spriteID, VineCharacterEmotion.Default), out _))
            return;//sprite already loaded
        foreach (VineCharacterEmotion emotion in Enum.GetValues(typeof(VineCharacterEmotion)))
        {
            string spriteFileName = spriteID + emotion;
            Sprite s = Resources.Load<Sprite>("CharacterSprites/" + spriteFileName);
            if (s == null)
                s = CharacterSpriteBank[(spriteID, VineCharacterEmotion.Default)];//if the character has no emotions, use the default one
            CharacterSpriteBank[(spriteID, emotion)] = s;
        }
    }
    Sprite LoadBackground(string id)
    {
        Sprite s = Resources.Load<Sprite>("Backgrounds/" + id);
        BackgroundSpritesBank.Add(id, s);
        return s;
    }
    public UniVineInteractionUI PlayInteraction(VinePassageMetadata metadata)
    {
        //metadata determines what kind of interaction
        return Instantiate(InteractionPrefab,transform);
    }
}