using Fusion;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ResultUIManager : MonoBehaviour
{
    public GameObject gameCanvas; // 게임 화면을 꺼야 하니까
    public GameObject resultCanvas;
    public GameObject mainMenuCanvas;

    public Text winnerText;
    public Text player1ResultText;
    public Text player2ResultText;
    public Button leaveButton;

    void Start()
    {
        leaveButton.onClick.AddListener(OnLeaveClicked);
    }

    public void ShowResult()
    {
        var players = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .ToList();

        if (players.Count < 2) return;

        var p1 = players[0];
        var p2 = players[1];

        player1ResultText.text = $"{p1.PlayerName} : {p1.TotalScore}점";
        player2ResultText.text = $"{p2.PlayerName} : {p2.TotalScore}점";

        if (p1.TotalScore == p2.TotalScore)
        {
            winnerText.text = "무승부";
        }
        else
        {
            var winner = p1.TotalScore > p2.TotalScore ? p1 : p2;
            winnerText.text = $"{winner.PlayerName} 승리!";
        }

        gameCanvas.SetActive(false);
        resultCanvas.SetActive(true);
    }

    async void OnLeaveClicked()
    {
        var roomManager = FindObjectOfType<RoomManager>();
        if (roomManager != null)
        {
            await roomManager.LeaveRoom();
            roomManager.ResetMenuButtons();
        }

        resultCanvas.SetActive(false);
        mainMenuCanvas.SetActive(true);
    }
}