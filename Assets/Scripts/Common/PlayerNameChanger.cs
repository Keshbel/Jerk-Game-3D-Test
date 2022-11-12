using Mirror;
using TMPro;
using UnityEngine;

public class PlayerNameChanger : MonoBehaviour
{
    public static string PlayerName;

    public TMP_InputField tmpInputField;

    private void OnDestroy()
    {
        if (PlayerName == "") ChangePlayerName("Player");
    }

    private void Start()
    {
        tmpInputField.onEndEdit.AddListener(ChangePlayerName);
    }

    private void ChangePlayerName(string playerName)
    {
        PlayerName = playerName;
        FindObjectOfType<NetworkRoomManager>().playerName = playerName;
    }
}
