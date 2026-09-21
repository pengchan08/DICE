using UnityEngine;
using System.Linq;

public class ComboUIManager : MonoBehaviour
{
    private ComboButtonHandler[] comboButtons;

    void Update()
    {
        comboButtons = FindObjectsOfType<ComboButtonHandler>();

        var myData = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);

        if (myData == null) return;

        var gameState = FindObjectOfType<GameStateManager>();
        bool isMyTurn = gameState != null
                        && gameState.FirstTurnDecided
                        && gameState.CurrentTurnPlayer == myData.Object.InputAuthority;

        bool isDiceRolling = DiceGroupController3D.Instance != null && DiceGroupController3D.Instance.IsAnyDiceRolling;

        bool canSelectCombo = isMyTurn && !isDiceRolling && myData.RollCount > 0
                              && myData.PendingCardAction == -1;

        foreach (var button in comboButtons)
        {
            button.Refresh(myData, canSelectCombo);
        }
    }
}