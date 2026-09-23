using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class FirstTurnUIManager : MonoBehaviour
{
    public Text player1RollText;
    public Text player2RollText;
    public Text resultText;

    void Update()
    {
        if (player1RollText == null || player2RollText == null || resultText == null) return;

        try
        {
            var players = FindObjectsOfType<PlayerData>()
                .Where(p => p != null && p.Object != null && p.Object.IsValid)
                .ToList();
            if (players.Count < 2) return;

            var gameState = FindObjectOfType<GameStateManager>();
            if (gameState == null || gameState.Object == null || !gameState.Object.IsValid) return;

            if (gameState.FirstTurnDecided) return;

            var p1 = players[0];
            var p2 = players[1];

            player1RollText.text = $"{p1.PlayerName} : {(p1.StartRoll == 0 ? "굴리는 중..." : p1.StartRoll.ToString())}";
            player2RollText.text = $"{p2.PlayerName} : {(p2.StartRoll == 0 ? "굴리는 중..." : p2.StartRoll.ToString())}";

            if (p1.StartRoll != 0 && p1.StartRoll == p2.StartRoll)
            {
                resultText.text = "무승부! 다시 굴립니다...";
            }
            else
            {
                resultText.text = "";
            }
        }
        catch (System.Exception)
        {
            // 스폰 처리 중 프레임 - 다음 프레임에 다시 시도
        }
    }

    public void HideTexts()
    {
        if (player1RollText != null) player1RollText.gameObject.SetActive(false);
        if (player2RollText != null) player2RollText.gameObject.SetActive(false);
        if (resultText != null) resultText.gameObject.SetActive(false);
    }

    public void ShowTexts()
    {
        if (player1RollText != null) player1RollText.gameObject.SetActive(true);
        if (player2RollText != null) player2RollText.gameObject.SetActive(true);
        if (resultText != null) resultText.gameObject.SetActive(true);
    }
}