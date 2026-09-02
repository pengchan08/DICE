using Fusion;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameUIManager : MonoBehaviour
{
    [Header("주사위 슬롯")]
    public Text[] diceSlotTexts; // 5개를 인스펙터에서 순서대로 연결
    public Text rollCountText;
    public Button rollDiceButton;

    public Text currentTurnText;
    public Button endTurnButton;

    public ComboButtonHandler[] comboButtons; // 10개를 인스펙터에서 연결

    [Header("3D 주사위")]
    public DiceController3D diceController;

    private GameStateManager _gameState;
    private NetworkRunner _runner;

    void Start()
    {
        endTurnButton.onClick.AddListener(OnEndTurnClicked);
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
            endTurnButton.interactable = isMyTurn;
            rollDiceButton.interactable = isMyTurn;
        }

        RefreshMySlots();
    }

    void RefreshMySlots()
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;

        bool isMyTurn = _gameState != null && _gameState.CurrentTurnPlayer == _runner.LocalPlayer;

        for (int i = 0; i < 5; i++)
        {
            diceSlotTexts[i].text = myData.DiceSlots[i].ToString();
        }
        rollCountText.text = $"남은 횟수: {8 - myData.RollCount} / 8";

        foreach (var combo in comboButtons)
        {
            combo.Refresh(myData, isMyTurn);
        }
    }

    void OnRollDiceClicked()
    {
        if (diceController != null)
        {
            diceController.RollDice();
        }
    }

    void OnEndTurnClicked()
    {
        if (_gameState != null)
        {
            _gameState.RPC_EndTurn();
        }
    }

    PlayerData FindMyPlayerData()
    {
        return FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);
    }
}