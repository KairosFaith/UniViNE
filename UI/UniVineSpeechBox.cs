using UnityEngine;
using TMPro;
using Vine;
using UnityEngine.UI;
public class UniVineSpeechBox : IUniVineTextBox
{
    public Transform PlayerPicAnchor, OppositePicAnchor;
    public TMP_Text Header;
    public override void InitiateBox(VineLineOutput line)
    {
        OnBoxOpen += () =>
        {
            VineLoader loader = UniVinePlayer.Instance.Loader;
            string lineCharacter = line.Character;
            Header.text = lineCharacter;
            if (loader.CharacterToSpriteLink.TryGetValue(lineCharacter, out _))
            {
                UniVinePlayer playerInstance = UniVinePlayer.Instance;
                //opposite character set scale to -1
                Transform picAnchor = OppositePicAnchor;
                bool playerTaking = lineCharacter == loader.CurrentPlayerCharacter;
                if (playerTaking)
                    picAnchor = PlayerPicAnchor;
                UniVinePortraitFrame frame = Instantiate(playerInstance.PortraitFramePrefab, picAnchor);
                frame.SetCharacterSprite(playerInstance.FetchCharacterSprite(line), playerTaking);
                FramesToShrink.Add(frame.transform);
            }
        };
        OnBoxClose += () => Header.text = "";
        base.InitiateBox(line);
    }
}