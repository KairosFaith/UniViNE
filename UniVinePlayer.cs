using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vine;
public class UniVinePlayer : MonoBehaviour//, IPointerDownHandler
{
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
    public void SetBackground(string id)
    {
        if (BackgroundSpritesBank.TryGetValue(id, out Sprite bg))
            BackgroundDisplay.sprite = bg;
        else
            BackgroundDisplay.sprite = LoadBackground(id);
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
    public void SetMusic(string id)
    {
        Source.Stop();
        Source.clip = Resources.Load<AudioClip>("Music/" + id);
        Source.Play();
    }
}