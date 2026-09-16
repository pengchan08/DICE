using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class OpponentBoardUIManager : MonoBehaviour
{
    [Header("상대 주사위 슬롯 5개 (읽기 전용)")]
    public Text[] opponentDiceValueTexts = new Text[5];

    [Header("상대 이름/현재 총점")]
    public Text opponentNameText;

    void Update()
    {
        var players = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .ToList();

        var opponentData = players.FirstOrDefault(p => !p.Object.HasStateAuthority);
        if (opponentData == null) return;

        if (opponentNameText != null)
        {
            opponentNameText.text = $"{opponentData.PlayerName} : {opponentData.TotalScore}점";
        }

        for (int i = 0; i < 5; i++)
        {
            if (opponentDiceValueTexts[i] == null) continue;
            int value = opponentData.DiceSlots[i];
            opponentDiceValueTexts[i].text = value == 0 ? "-" : value.ToString();
        }
    }
}