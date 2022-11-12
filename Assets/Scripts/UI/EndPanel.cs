using TMPro;
using UnityEngine;

public class EndPanel : MonoBehaviour
{
    public GameObject panel;
    [SerializeField] TMP_Text winnerText;

    public void SetWinnerText(string text)
    {
        winnerText.text = text;
        panel.SetActive(true);
    }
}
