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
        var players = FindObjectsOfType<PlayerData>();
        if (players.Length < 2) return;

        var p1 = players[0];
        var p2 = players[1];

        player1RollText.text = $"{p1.PlayerName} : {(p1.StartRoll == 0 ? "굴리는 중..." : p1.StartRoll.ToString())}";
        player2RollText.text = $"{p2.PlayerName} : {(p2.StartRoll == 0 ? "굴리는 중..." : p2.StartRoll.ToString())}";

        var gameState = FindObjectOfType<GameStateManager>();
        if (gameState != null && gameState.FirstTurnDecided)
        {
            var winner = players.FirstOrDefault(p => p.Object.InputAuthority == gameState.CurrentTurnPlayer);
            resultText.text = winner != null ? $"{winner.PlayerName} 선공!" : "";
        }
        else if (p1.StartRoll != 0 && p1.StartRoll == p2.StartRoll)
        {
            resultText.text = "무승부! 다시 굴립니다...";
        }
        else
        {
            resultText.text = "";
        }
    }
}