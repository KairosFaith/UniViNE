using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using Vine;
using System.Collections;
public class UniVinePlayer : MonoBehaviour, VinePlayer
{
    public static UniVinePlayer Instance;
    private void Awake()
    {
        if(Instance==null)
        Instance = this;
    }
    private void OnDestroy()
    {
        if (Instance == this)
        Instance = null;
    }
    const string//folder names
        Narration = "Narration",
        CharacterSprites = "CharacterSprites",
        Backgrounds = "Backgrounds",
        Music = "Music";
    //Slash = "/";
    public float LetterRate = .1f, BoxOpenCloseRate = .1f, BackgroundShiftRate;
    public AudioMixerGroup MusicChannel;
    public Image BackgroundDisplay;
    public RectTransform BGAnchor1, BGAnchor2;
    [Header("Prefabs")]
    public UniVineSpeechBox SpeechBoxPrefab;
    public UniVineNarrationBox NarrationBoxPrefab;
    public UniVineInteractionUI InteractionPrefab;
    public UniVinePortraitFrame PortraitFramePrefab;
    public UniVineTitleFrame TitleFramePrefab;
    public VineLoader Loader { get; set; }
    AudioSource _CurrentMusicSource;
    Dictionary<string, Sprite> _BackgroundSpritesBank = new Dictionary<string, Sprite>();
    Dictionary<(string, UniVineCharacterEmotion), Sprite> _CharacterSpriteBank = new Dictionary<(string, UniVineCharacterEmotion), Sprite>();
    Sprite _LastFetchedPlayerSprite;//for use in interactions
    void Start()//for testing
    {
        Loader = new VineLoader(this);
        Loader.StartStory("Prologue");
    }
    public void OutputLine(VineLineOutput line)
    {
        IUniVineTextBox b;
        string lineCharacter = line.Character;
        bool isPlayer = lineCharacter == Loader.CurrentPlayerCharacter;
        if (lineCharacter == Narration)
        {
            b = Instantiate(NarrationBoxPrefab, transform);
            b.InitiateBox(line);
        }
        else
        {
            void openSpeechBox()
            {
                b = Instantiate(SpeechBoxPrefab, transform);
                b.InitiateBox(line);
            };
            ShiftBackground(lineCharacter, openSpeechBox);
        }
    }
    void ShiftBackground(string lineCharacter, Action onDone)
    {
        Vector2 targetPosition;
        if(lineCharacter == Loader.CurrentPlayerCharacter)
            targetPosition = BGAnchor1.position;
        else
            targetPosition = BGAnchor2.position;
        StartCoroutine(ShiftBackgroundRoutine(targetPosition, onDone));
    }
    IEnumerator ShiftBackgroundRoutine(Vector2 targetPosition, Action onDone)
    {
        for (float t = 0; t <= BackgroundShiftRate; t += Time.deltaTime)
        {
            Vector2 curPos = BackgroundDisplay.rectTransform.position;
            BackgroundDisplay.rectTransform.position = Vector2.Lerp(curPos, targetPosition, t / BackgroundShiftRate);
            yield return new WaitForEndOfFrame();
        }
        onDone();//non null
    }
    public void SetCharacterSprite(string characterKeyValuePair)
    {
        string[] args = characterKeyValuePair.Split('=');
        string spriteID = args[1];
        Loader.CharacterToSpriteLink[args[0]] = args[1];
        foreach (UniVineCharacterEmotion emotion in Enum.GetValues(typeof(UniVineCharacterEmotion)))
        {
            string spriteFileName = spriteID + '_' + emotion;
            Sprite s = Resources.Load<Sprite>(CharacterSprites + "/" + spriteFileName) ?? _CharacterSpriteBank[(spriteID, UniVineCharacterEmotion.neutral)];//if no sprite for emotion, use default
            _CharacterSpriteBank.Add((spriteID, emotion), s);
        }
    }
    public void SetBackground(string id)
    {
        if (_BackgroundSpritesBank.TryGetValue(id, out Sprite bg))
            BackgroundDisplay.sprite = bg;
        else
            BackgroundDisplay.sprite = LoadBackground(id);
    }
    public void PlayMusic(string id)
    {
        if (_CurrentMusicSource)
        {
            _CurrentMusicSource.Stop();
            Destroy(_CurrentMusicSource.gameObject);
        }
        GameObject go = new GameObject("MusicSource"+id);
        _CurrentMusicSource = go.AddComponent<AudioSource>();
        _CurrentMusicSource.clip = Resources.Load<AudioClip>(Music+"/" + id);
        _CurrentMusicSource.loop = true;
        _CurrentMusicSource.Play();
    }
    public void SetPlayerCharacter(string characterName)
    {
        Loader.CurrentPlayerCharacter = characterName;
    }
    public Sprite FetchCharacterSprite(VineLineOutput line)
    {
        string lineCharacter = line.Character;
        string spriteID = Loader.CharacterToSpriteLink[lineCharacter];
        Sprite s = _CharacterSpriteBank[(spriteID, line.Emotion)];
        if (lineCharacter == Loader.CurrentPlayerCharacter)
            _LastFetchedPlayerSprite = s;
        return s;
    }
    Sprite LoadBackground(string id)
    {
        Sprite s = Resources.Load<Sprite>(Backgrounds + "/" + id);
        _BackgroundSpritesBank.Add(id, s);
        return s;
    }
    public void ShowTitle(VineHeaderOutput header)
    {
        UniVineTitleFrame frame = Instantiate(TitleFramePrefab, transform);
        frame.SetHeader(header);
    }
    public VineInteraction PlayInteraction(VinePassageMetadata metadata)
    {
        //TODO metadata determines what kind of interaction
        UniVineInteractionUI ui = Instantiate(InteractionPrefab, transform);
        ui.Initialise(metadata, _LastFetchedPlayerSprite);
        return ui;
    }
}
//iphone 5, camera size 3.60315883159637428 + 1-epsilon
//iphone 4, perfect fit, cant move background