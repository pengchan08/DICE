using Fusion;
using UnityEngine;
using System.Linq;

public class GameStateManager : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnCurrentTurnChanged))]
    public PlayerRef CurrentTurnPlayer { get; set; }

    [Networked, OnChangedRender(nameof(OnFirstTurnDecidedChanged))]
    public NetworkBool FirstTurnDecided { get; set; }

    [Networked] public NetworkBool RerollInProgress { get; set; }
    [Networked] public TickTimer RerollDelayTimer { get; set; }
    [Networked] public NetworkBool WaitingForGuestRoll { get; set; }

    [Networked] public int LastComparedHostVersion { get; set; }
    [Networked] public int LastComparedGuestVersion { get; set; }

    [Networked, OnChangedRender(nameof(OnGameEndedChanged))]
    public NetworkBool GameEnded { get; set; }

    private const float TieRevealSeconds = 1.5f;

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (!FirstTurnDecided)
        {
            HandleFirstTurnDecision();
            return;
        }

        if (!GameEnded)
        {
            CheckGameEnd();
        }
    }

    void HandleFirstTurnDecision()
    {
        var players = FindObjectsOfType<PlayerData>()
             .Where(p => p != null && p.Object != null && p.Object.IsValid)
             .ToList();
        if (players.Count < 2) return;

        var host = players.FirstOrDefault(p => p.IsHost);
        var guest = players.FirstOrDefault(p => !p.IsHost);
        if (host == null || guest == null) return;

        if (RerollInProgress)
        {
            if (RerollDelayTimer.Expired(Runner))
            {
                RerollInProgress = false;
                WaitingForGuestRoll = false;
                RPC_ResetAndRollHostFirst();
            }
            return;
        }

        bool hostHasFreshRoll = host.StartRoll != 0 && host.StartRollVersion > LastComparedHostVersion;
        bool guestHasFreshRoll = guest.StartRoll != 0 && guest.StartRollVersion > LastComparedGuestVersion;

        if (!hostHasFreshRoll)
        {
            return;
        }

        if (!guestHasFreshRoll)
        {
            if (!WaitingForGuestRoll)
            {
                WaitingForGuestRoll = true;
                RPC_TriggerRoll(guest.Object.InputAuthority);
            }
            return;
        }

        LastComparedHostVersion = host.StartRollVersion;
        LastComparedGuestVersion = guest.StartRollVersion;
        WaitingForGuestRoll = false;

        if (host.StartRoll == guest.StartRoll)
        {
            RerollInProgress = true;
            RerollDelayTimer = TickTimer.CreateFromSeconds(Runner, TieRevealSeconds);
            return;
        }

        var winner = host.StartRoll > guest.StartRoll ? host : guest;
        CurrentTurnPlayer = winner.Object.InputAuthority;
        FirstTurnDecided = true;
    }

    void CheckGameEnd()
    {
        var players = FindObjectsOfType<PlayerData>();
        if (players.Length < 2) return;

        if (players[0].AreAllCombosUsed() && players[1].AreAllCombosUsed())
        {
            GameEnded = true;
        }
    }

    void OnGameEndedChanged()
    {
        if (!GameEnded) return;
        var resultUI = FindObjectOfType<ResultUIManager>(true);
        if (resultUI != null) resultUI.ShowResult();
    }

    void OnFirstTurnDecidedChanged()
    {
        if (!FirstTurnDecided) return;

        if (DiceGroupController3D.Instance != null)
        {
            DiceGroupController3D.Instance.ActivateGameplayDice();
        }

        var firstTurnUI = FindObjectOfType<FirstTurnUIManager>(true);
        if (firstTurnUI != null) firstTurnUI.HideTexts();
    }

    void OnCurrentTurnChanged()
    {
        var uiManager = FindObjectOfType<GameUIManager>();
        if (uiManager != null) uiManager.RefreshTurnUI();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_TriggerRoll(PlayerRef target)
    {
        if (Runner.LocalPlayer != target) return;
        var dice = DiceGroupController3D.Instance != null ? DiceGroupController3D.Instance.FirstTurnDice : null;
        if (dice != null) dice.RequestAndRoll();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ResetAndRollHostFirst()
    {
        var myData = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);

        if (myData == null) return;

        myData.ResetStartRollOnly();

        if (myData.IsHost)
        {
            var dice = DiceGroupController3D.Instance != null ? DiceGroupController3D.Instance.FirstTurnDice : null;
            if (dice != null) dice.RequestAndRoll();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_EndTurn()
    {
        var players = FindObjectsOfType<PlayerData>();
        var other = players.FirstOrDefault(p => p.Object.InputAuthority != CurrentTurnPlayer);

        if (other != null)
        {
            CurrentTurnPlayer = other.Object.InputAuthority;
        }
    }
}