using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vine;
public class UniVinePlayer : MonoBehaviour
{
    const string//folder names
        Narration = "Narration",
        CharacterSprites = "CharacterSprites",
        Backgrounds = "Backgrounds",
        Music = "Music";
        //Slash = "/";
    public UniVineSpeechBox PlayerSpeechBoxPrefab, OppositeSpeechBoxPrefab;
    public TMP_Text NarrationBox;
    public Transform SpeechBoxAnchor;
    public UniVineInteractionUI InteractionPrefab;
    public AudioSource Source;
    public Image BackgroundDisplay;
    Dictionary<string, Sprite> _BackgroundSpritesBank = new Dictionary<string, Sprite>();
    Dictionary<(string, VineCharacterEmotion), Sprite> _CharacterSpriteBank = new Dictionary<(string, VineCharacterEmotion), Sprite>();
    //Data To Save
    Dictionary<string, string> _CharacterToSpriteLink = new Dictionary<string, string>();
    GameObject _ObjectToClear;
    public TMP_Text OutputLine(VineLineOutput line, UniVineLoader loader)
    {
        string lineCharacter = line.Character;
        UniVineSpeechBox boxToSpawn = OppositeSpeechBoxPrefab;
        if (lineCharacter == loader.CurrentPlayerCharacter)
            boxToSpawn = PlayerSpeechBoxPrefab;
        else if (lineCharacter == Narration)
        {
            var o = Instantiate(NarrationBox, transform);
            o.text = line.Text;
            _ObjectToClear = o.gameObject;
            return o;
        }
        UniVineSpeechBox b = Instantiate(boxToSpawn, SpeechBoxAnchor);
        _ObjectToClear = b.gameObject;
        Sprite characterSprite = GetCharacterSprite(line);
        return b.SetSpeechBox(line, characterSprite);
    }
    public void ProcessMarker(VineMarkedOutput mark, UniVineLoader loader)
    {
        switch (mark.MarkType)
        {
            case VineMarkType.Music:
                SetMusic(mark.Text[0]);
                break;
            case VineMarkType.Background:
                SetBackground(mark.Text[0]);
                break;
            case VineMarkType.SetPlayerCharacter:
                loader.CurrentPlayerCharacter = mark.Text[0];
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
        _CharacterToSpriteLink[args[0]] = args[1];
        if (_CharacterSpriteBank.TryGetValue((spriteID, VineCharacterEmotion.Default), out _))
            return;//sprite already loaded
        foreach (VineCharacterEmotion emotion in Enum.GetValues(typeof(VineCharacterEmotion)))
        {
            string spriteFileName = spriteID + emotion;
            Sprite s = Resources.Load<Sprite>(CharacterSprites + "/" + spriteFileName);
            if (s == null)
                s = _CharacterSpriteBank[(spriteID, VineCharacterEmotion.Default)];//if the character has no emotions, use the default one
            _CharacterSpriteBank[(spriteID, emotion)] = s;
        }
    }
    Sprite GetCharacterSprite(VineLineOutput line)
    {
        string spriteID = _CharacterToSpriteLink[line.Character];
        return _CharacterSpriteBank[(spriteID, line.Emotion)];
    }
    void SetBackground(string id)
    {
        if (_BackgroundSpritesBank.TryGetValue(id, out Sprite bg))
            BackgroundDisplay.sprite = bg;
        else
            BackgroundDisplay.sprite = LoadBackground(id);
    }
    Sprite LoadBackground(string id)
    {
        Sprite s = Resources.Load<Sprite>(Backgrounds + "/" + id);
        _BackgroundSpritesBank.Add(id, s);
        return s;
    }
    public UniVineInteractionUI PlayInteraction(VinePassageMetadata metadata)
    {
        //metadata determines what kind of interaction
        return Instantiate(InteractionPrefab,transform);
    }
    void SetMusic(string id)
    {
        Source.Stop();
        Source.clip = Resources.Load<AudioClip>(Music+"/" + id);
        Source.Play();
    }
    public void ClearPrevObject()
    {
        Destroy(_ObjectToClear);
    }
}