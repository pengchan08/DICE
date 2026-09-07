using UnityEngine;
using System.Linq;

public class ComboUIManager : MonoBehaviour
{
    private ComboButtonHandler[] comboButtons;

    void Start()
    {
        // 씬에 있는 12개 조합 버튼을 자동으로 찾음
        comboButtons = FindObjectsOfType<ComboButtonHandler>();
    }

    void Update()
    {
        var myData = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);

        if (myData == null) return;

        var gameState = FindObjectOfType<GameStateManager>();
        bool isMyTurn = gameState != null
                        && gameState.FirstTurnDecided
                        && gameState.CurrentTurnPlayer == myData.Object.InputAuthority;

        // 추가: 이번 턴에 한 번도 안 굴렸으면 조합 선택도 막음 (굴리기 전에 확정하는 건 말이 안 되니까)
        bool canSelectCombo = isMyTurn && myData.RollCount > 0;

        foreach (var button in comboButtons)
        {
            button.Refresh(myData, canSelectCombo);
        }
    }
}