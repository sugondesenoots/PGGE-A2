using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameInput : MonoBehaviour
{
    private InputField mInputField;
    const string playerNamePrefKey = "PlayerName";

    void Start()
    {
        mInputField = this.GetComponent<InputField>();

        string defaultName = string.Empty; 

        if (mInputField != null)
        {
            if (PlayerPrefs.HasKey(playerNamePrefKey))
            {
                defaultName = PlayerPrefs.GetString(playerNamePrefKey);
                mInputField.text = defaultName;
            }
        } 

        PhotonNetwork.NickName = defaultName;

        //Looks for PlayerUsername script
        //When found, calls UpdatePlayerNameText function
        PlayerUsername playerUsername = FindObjectOfType<PlayerUsername>(); 

        if (playerUsername != null)
        {
            playerUsername.UpdatePlayerUsernameText(defaultName);
        }
    }

    public void SetPlayerName()
    {
        string value = mInputField.text; 

        if (string.IsNullOrEmpty(value))
        {
            Debug.LogError("Player Name is null or empty");
            return;
        } 

        PhotonNetwork.NickName = value;
        PlayerPrefs.SetString(playerNamePrefKey, value);

        //Same usage above
        PlayerUsername playerUsername = FindObjectOfType<PlayerUsername>(); 

        if (playerUsername != null)
        {
            playerUsername.UpdatePlayerUsernameText(value);
        } 

        Debug.Log("Nickname entered: " + value);
    }
}
