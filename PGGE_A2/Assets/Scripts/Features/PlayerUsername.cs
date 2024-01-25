using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUsername : MonoBehaviourPunCallbacks
{
    private Text playerUsernameText;

    void Start()
    {
        playerUsernameText = GetComponent<Text>();
        if (playerUsernameText == null)
        {
            Debug.LogError("PlayerUsernameText component not found.");
        }

        //Calls function to set the player's username
        SetPlayerUsername();
    }

    public void SetPlayerUsername()
    { 
        if (photonView.IsMine)
        {
            //Gets preferred player username from PlayerPrefs
            //PlayerPrefs holds player preferences
            string playerName = PlayerPrefs.GetString("PlayerName");

            //Updates player username 
            UpdatePlayerUsernameText(playerName);
        }
    }

    [PunRPC]
    public void UpdatePlayerUsernameText(string playerName)
    {
        if (playerUsernameText != null)
        {
            playerUsernameText.text = "" + playerName;
        }
        else
        {
            Debug.LogError("PlayerNameText component not found on the object.");
        }
    }
}
