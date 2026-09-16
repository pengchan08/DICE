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

    // 능력 카드 선택 단계
    [Networked, OnChangedRender(nameof(OnCardSelectionPendingChanged))]
    public NetworkBool CardSelectionPending { get; set; }
    [Networked] public int CardOptionA { get; set; }
    [Networked] public int CardOptionB { get; set; }

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

        // 능력 카드 2장을 랜덤으로 뽑아 선공에게 선택권 부여 (서로 다른 종류로)
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

        // 주사위 스폰은 카드 선택이 끝난 뒤(OnCardSelectionPendingChanged)에 진행
    }

    void OnCardSelectionPendingChanged()
    {
        if (CardSelectionPending) return; // 선택 시작 시점엔 할 일 없음 (UI가 알아서 표시)
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

        var myData = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);

        if (myData != null && myData.Object.InputAuthority == CurrentTurnPlayer)
        {
            myData.ResetBoardForNewTurn();
        }
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

    // 선공 플레이어가 카드를 선택했을 때 클라이언트가 호출 (호스트에서 검증 후 배정)
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSelectCard(PlayerRef requester, int chosenCardType)
    {
        if (!CardSelectionPending) return;
        if (requester != CurrentTurnPlayer) return; // 선공만 선택 가능
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

    // 각자 자기 자신의 PlayerData에만 실제로 카드를 배정 (StateAuthority 규칙 준수)
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
}