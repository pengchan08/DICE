using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class OpponentBoardUIManager : MonoBehaviour
{
    [Header("상대 턴 UI 루트 (상대 턴에만 활성화)")]
    public GameObject opponentBoardPanel;

    [Header("상대 주사위 슬롯 5개 (읽기 전용)")]
    public Text[] opponentDiceValueTexts = new Text[5];

    void Update()
    {
        var players = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .ToList();

        var myData = players.FirstOrDefault(p => p.Object.HasStateAuthority);
        var opponentData = players.FirstOrDefault(p => !p.Object.HasStateAuthority);
        if (myData == null || opponentData == null) return;

        var gameState = FindObjectOfType<GameStateManager>();
        bool isMyTurn = gameState != null && gameState.FirstTurnDecided
                        && gameState.CurrentTurnPlayer == myData.Object.InputAuthority;

        if (opponentBoardPanel != null) opponentBoardPanel.SetActive(!isMyTurn);
        if (isMyTurn) return;

        for (int i = 0; i < 5; i++)
        {
            if (opponentDiceValueTexts[i] == null) continue;
            int value = opponentData.DiceSlots[i];
            opponentDiceValueTexts[i].text = value == 0 ? "-" : value.ToString();
        }
    }
}