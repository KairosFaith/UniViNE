using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vine;
public class UniVinePlayer : MonoBehaviour//, IPointerDownHandler
{
<<<<<<< HEAD
    public TMP_Text TextBoxPrefab;
    public Image CharacterDisplayPrefab;
    public Transform PlayerDisplayAnchor, OppositeDisplayAnchor;
    public UniVineInteractionUI InteractionPrefab;
    public AudioSource Source;
    public Image BackgroundDisplay;
    Dictionary<string, Sprite> BackgroundSpritesBank = new Dictionary<string, Sprite>();
    Dictionary<(string, VineCharacterEmotion), Sprite> CharacterSpriteBank = new Dictionary<(string, VineCharacterEmotion), Sprite>();
    public TMP_Text OutputLine(VineLineOutput line, UniVineLoader loader)
    {
        //TMP_Text textBox = null;
        //textBox = Player.OutputLine(line,this);
        //string show = textBox.text = line.Text;
        //int stringLength = show.Length;
        //for (int i = 0;i< stringLength; i++)
        //{
        //    textBox.maxVisibleCharacters = i;
        //    yield return new WaitForSeconds(.1f);
        //    if (_Pressed)
        //    {
        //        textBox.maxVisibleCharacters = stringLength;
        //        _Pressed = false;
        //        break;
        //    }
        //}


        string whosTalking = line.Character;
        if (whosTalking == "Narration")
        {
            //No character sprite, huge text box on the top
        }
        else
        {
            Transform anchor = null;
            bool isPlayer = whosTalking == loader.CurrentPlayerCharacter;
            if (isPlayer)
                anchor = PlayerDisplayAnchor;//Image on left side
            else
                anchor = OppositeDisplayAnchor;//Image on right side
            Image image = Instantiate(CharacterDisplayPrefab, anchor);
            image.sprite = CharacterSpriteBank[(loader.CharacterToSpriteLink[whosTalking], line.Emotion)];
            if (!isPlayer)
                image.transform.localScale = new Vector3(-1, 1, 1);//TODO can do this using the anchor instead? hmmm hope i can omit this line
        }

        var t = Instantiate(TextBoxPrefab, transform);
        t.maxVisibleCharacters = 0;
        return t;
        
        //_Pressed = false;
        //Destroy(textBox.gameObject);
    }
    public void LoadCharacterSprites(VineMarkedOutput mark, UniVineLoader loader)
    {
        string[] args = mark.Text;
        string spriteID = args[1];
        loader.CharacterToSpriteLink[args[0]] = args[1];
        if (CharacterSpriteBank.TryGetValue((spriteID, VineCharacterEmotion.Default), out _))
=======
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
>>>>>>> 3ac408fac85cc3d344a5afcca5c04fb29212791a
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
<<<<<<< HEAD
    public void SetBackground(string id)
    {
        if (BackgroundSpritesBank.TryGetValue(id, out Sprite bg))
=======
    Sprite GetCharacterSprite(VineLineOutput line)
    {
        string spriteID = _CharacterToSpriteLink[line.Character];
        return _CharacterSpriteBank[(spriteID, line.Emotion)];
    }
    void SetBackground(string id)
    {
        if (_BackgroundSpritesBank.TryGetValue(id, out Sprite bg))
>>>>>>> 3ac408fac85cc3d344a5afcca5c04fb29212791a
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
<<<<<<< HEAD
    public void SetMusic(string id)
    {
        Source.Stop();
        Source.clip = Resources.Load<AudioClip>("Music/" + id);
        Source.Play();
    }
=======
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
>>>>>>> 3ac408fac85cc3d344a5afcca5c04fb29212791a
}