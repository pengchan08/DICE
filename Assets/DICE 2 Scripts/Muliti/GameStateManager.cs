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

    [Networked, OnChangedRender(nameof(OnCardSelectionPendingChanged))]
    public NetworkBool CardSelectionPending { get; set; }
    [Networked] public int CardOptionA { get; set; }
    [Networked] public int CardOptionB { get; set; }

    [Header("다시하기용 주사위 프리팹 (재시작 시 사용)")]
    public NetworkPrefabRef firstTurnDicePrefabForRematch;

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
        else
        {
            CheckRematch();
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

        int optionA = Random.Range(0, 4);
        int optionB;
        do { optionB = Random.Range(0, 4); } while (optionB == optionA);
        CardOptionA = optionA;
        CardOptionB = optionB;
        CardSelectionPending = true;
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

    void CheckRematch()
    {
        var players = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .ToList();

        if (players.Count < 2) return;
        if (!players.All(p => p.WantsRematch)) return;

        RPC_StartRematch();
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

        var firstTurnUI = FindObjectOfType<FirstTurnUIManager>(true);
        if (firstTurnUI != null) firstTurnUI.HideTexts();
    }

    void OnCardSelectionPendingChanged()
    {
        if (CardSelectionPending) return;
        if (!FirstTurnDecided) return;

        if (DiceGroupController3D.Instance != null)
        {
            DiceGroupController3D.Instance.ActivateGameplayDice();
        }
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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSelectCard(PlayerRef requester, int chosenCardType)
    {
        if (!CardSelectionPending) return;
        if (requester != CurrentTurnPlayer) return;
        if (chosenCardType != CardOptionA && chosenCardType != CardOptionB) return;

        int otherCardType = (chosenCardType == CardOptionA) ? CardOptionB : CardOptionA;

        var players = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .ToList();
        var otherPlayerData = players.FirstOrDefault(p => p.Object.InputAuthority != CurrentTurnPlayer);
        PlayerRef otherPlayer = otherPlayerData != null ? otherPlayerData.Object.InputAuthority : PlayerRef.None;

        RPC_AssignAbilityCards(CurrentTurnPlayer, chosenCardType, otherPlayer, otherCardType);

        CardSelectionPending = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_AssignAbilityCards(PlayerRef winnerPlayer, int winnerCardType, PlayerRef otherPlayer, int otherCardType)
    {
        var myData = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);

        if (myData == null) return;

        if (myData.Object.InputAuthority == winnerPlayer)
        {
            myData.AssignCard(winnerCardType);
        }
        else if (myData.Object.InputAuthority == otherPlayer)
        {
            myData.AssignCard(otherCardType);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_StartRematch()
    {
        // 각 클라이언트가 자기 자신의 데이터만 리셋
        var myData = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);

        if (myData != null)
        {
            myData.ResetForRematch();
        }

        // 화면 전환은 모든 클라이언트에서 로컬로 수행
        var resultUI = FindObjectOfType<ResultUIManager>(true);
        if (resultUI != null) resultUI.HideResultShowGame();

        var firstTurnUI = FindObjectOfType<FirstTurnUIManager>(true);
        if (firstTurnUI != null) firstTurnUI.ShowTexts();

        // 게임 상태 및 주사위 재생성은 호스트만 실제로 반영
        if (Object.HasStateAuthority)
        {
            GameEnded = false;
            FirstTurnDecided = false;
            RerollInProgress = false;
            WaitingForGuestRoll = false;
            LastComparedHostVersion = 0;
            LastComparedGuestVersion = 0;
            CardSelectionPending = false;

            if (DiceGroupController3D.Instance != null)
            {
                DiceGroupController3D.Instance.ResetForRematch();
            }
        }
    }
}