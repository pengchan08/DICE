using Fusion;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameUIManager : MonoBehaviour
{
    [Header("주사위 슬롯")]
    public Text[] diceSlotTexts;
    public Button rollDiceButton;

    public Text currentTurnText;

    [Header("3D 주사위")]
    public DiceController3D diceController;

    [Header("점수 표시")]
    public Text player1ScoreText;
    public Text player2ScoreText;

    [Header("내 점수 강조 색상")]
    public Color myScoreColor = Color.yellow;
    public Color opponentScoreColor = Color.white;

    private GameStateManager _gameState;
    private NetworkRunner _runner;

    void Start()
    {
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        _runner = FindObjectOfType<NetworkRunner>();
    }

    void Update()
    {
        if (_gameState == null)
        {
            _gameState = FindObjectOfType<GameStateManager>();
        }

        RefreshMySlots();
    }

    public void RefreshTurnUI()
    {
        if (_gameState == null || _runner == null) return;

        var currentPlayerData = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.InputAuthority == _gameState.CurrentTurnPlayer);

        if (currentPlayerData != null)
        {
            bool isMyTurn = _gameState.CurrentTurnPlayer == _runner.LocalPlayer;
            currentTurnText.text = isMyTurn ? "현재 턴: 내 턴" : $"현재 턴: {currentPlayerData.PlayerName}";
            rollDiceButton.interactable = isMyTurn;
        }

        RefreshMySlots();
    }

    void RefreshMySlots()
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;

        for (int i = 0; i < 5; i++)
        {
            diceSlotTexts[i].text = myData.DiceSlots[i].ToString();
        }

        RefreshScores();
    }

    void RefreshScores()
    {
        var players = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .ToList();

        if (players.Count < 1) return;

        var p1 = players.FirstOrDefault(p => p.IsHost);
        var p2 = players.FirstOrDefault(p => !p.IsHost);

        if (p1 != null)
        {
            player1ScoreText.text = $"{p1.PlayerName} : {p1.TotalScore}점";
            player1ScoreText.color = p1.Object.HasStateAuthority ? myScoreColor : opponentScoreColor;
        }
        if (p2 != null)
        {
            player2ScoreText.text = $"{p2.PlayerName} : {p2.TotalScore}점";
            player2ScoreText.color = p2.Object.HasStateAuthority ? myScoreColor : opponentScoreColor;
        }
    }

    void OnRollDiceClicked()
    {
        if (diceController != null)
        {
            DiceGroupController3D.Instance.RollNonHeldDice();
        }
    }

    PlayerData FindMyPlayerData()
    {
        return FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);
    }
}