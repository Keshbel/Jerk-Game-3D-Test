using System.Collections;
using Mirror;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region SingleTone
    public static GameManager Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    #endregion

    public EndPanel endPanel;

    public IEnumerator PlayerWin(MyPlayerController myPlayer)
    {
        endPanel.SetWinnerText("Winner = " + myPlayer.playerName);
        yield return new WaitForSeconds(5f);
        NetworkRoomManager.singleton.ServerChangeScene(NetworkRoomManager.networkSceneName);
    }
}
