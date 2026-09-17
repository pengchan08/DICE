using Fusion;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ResultUIManager : MonoBehaviour
{
    public GameObject gameCanvas;
    public GameObject resultCanvas;
    public GameObject mainMenuCanvas;

    public Text winnerText;

    [Header("플레이어 1")]
    public Text player1NameText;
    public Text player1ScoreText;

    [Header("플레이어 2")]
    public Text player2NameText;
    public Text player2ScoreText;

    [Header("내 이름 강조 색상")]
    public Color myNameColor = Color.yellow;
    public Color opponentNameColor = Color.white;

    [Header("다시하기")]
    public Button rematchButton;
    public Text rematchStatusText;

    public Button leaveButton;

    void Start()
    {
        leaveButton.onClick.AddListener(OnLeaveClicked);
        if (rematchButton != null) rematchButton.onClick.AddListener(OnRematchClicked);
    }

    void Update()
    {
        if (resultCanvas == null || !resultCanvas.activeSelf) return;

        var players = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .ToList();

        int readyCount = players.Count(p => p.WantsRematch);
        if (rematchStatusText != null)
        {
            rematchStatusText.text = $"다시하기 : {readyCount} / 2";
        }

        var myData = players.FirstOrDefault(p => p.Object.HasStateAuthority);
        if (myData != null && rematchButton != null)
        {
            rematchButton.interactable = !myData.WantsRematch;
        }
    }

    public void ShowResult()
    {
        var players = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .ToList();

        if (players.Count < 2) return;

        var p1 = players[0];
        var p2 = players[1];

        player1NameText.text = p1.PlayerName.ToString();
        player1NameText.color = p1.Object.HasStateAuthority ? myNameColor : opponentNameColor;
        player1ScoreText.text = $"{p1.TotalScore}점";

        player2NameText.text = p2.PlayerName.ToString();
        player2NameText.color = p2.Object.HasStateAuthority ? myNameColor : opponentNameColor;
        player2ScoreText.text = $"{p2.TotalScore}점";

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

    public void HideResultShowGame()
    {
        resultCanvas.SetActive(false);
        gameCanvas.SetActive(true);
    }

    void OnRematchClicked()
    {
        var myData = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);

        if (myData != null)
        {
            myData.RequestRematch();
        }
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