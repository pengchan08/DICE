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
    [Networked] public TickTimer RerollTimer { get; set; }

    [Networked] public int LastComparedP1Version { get; set; }
    [Networked] public int LastComparedP2Version { get; set; }

    [Networked, OnChangedRender(nameof(OnGameEndedChanged))]
    public NetworkBool GameEnded { get; set; }

    private const float TieRevealSeconds = 3f;

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
        var players = FindObjectsOfType<PlayerData>();
        if (players.Length < 2) return;

        var p1 = players[0];
        var p2 = players[1];

        if (RerollInProgress)
        {
            if (RerollTimer.Expired(Runner))
            {
                RerollInProgress = false;
                RPC_RequestReroll();
            }
            return;
        }

        bool p1HasFreshRoll = p1.StartRoll != 0 && p1.StartRollVersion > LastComparedP1Version;
        bool p2HasFreshRoll = p2.StartRoll != 0 && p2.StartRollVersion > LastComparedP2Version;

        if (!p1HasFreshRoll || !p2HasFreshRoll) return;

        LastComparedP1Version = p1.StartRollVersion;
        LastComparedP2Version = p2.StartRollVersion;

        if (p1.StartRoll == p2.StartRoll)
        {
            RerollInProgress = true;
            RerollTimer = TickTimer.CreateFromSeconds(Runner, TieRevealSeconds);
            Debug.Log($"[선공 결정] 동점! ({p1.StartRoll}) - {TieRevealSeconds}초 후 다시 굴립니다.");
            return;
        }

        var winner = p1.StartRoll > p2.StartRoll ? p1 : p2;
        CurrentTurnPlayer = winner.Object.InputAuthority;
        FirstTurnDecided = true;

        Debug.Log($"[선공 결정] {winner.PlayerName} 이(가) 선공입니다. ({p1.PlayerName} 롤: {p1.StartRoll}, {p2.PlayerName} 롤: {p2.StartRoll})");
    }

    void CheckGameEnd()
    {
        var players = FindObjectsOfType<PlayerData>();
        if (players.Length < 2) return;

        if (players[0].AreAllCombosUsed() && players[1].AreAllCombosUsed())
        {
            GameEnded = true;
            Debug.Log("[게임 종료] 두 플레이어 모두 모든 조합을 사용했습니다.");
        }
    }

    void OnGameEndedChanged()
    {
        if (!GameEnded) return;

        var resultUI = FindObjectOfType<ResultUIManager>(true); // true 추가: 비활성화된 것도 찾기
        if (resultUI != null)
        {
            resultUI.ShowResult();
        }
        else
        {
            Debug.LogWarning("[디버그] ResultUIManager를 찾지 못했습니다.");
        }
    }

    void OnFirstTurnDecidedChanged()
    {
        if (!FirstTurnDecided) return;

        var winnerData = FindObjectsOfType<PlayerData>()
            .FirstOrDefault(p => p.Object.InputAuthority == CurrentTurnPlayer);

        if (winnerData != null)
        {
            Debug.Log($"[선공 결정] {winnerData.PlayerName} 이(가) 선공입니다!");
        }

        // 변경: 배열을 직접 안 들고, 씬에 있는 매니저를 찾아서 호출
        if (DiceGroupController3D.Instance != null)
        {
            DiceGroupController3D.Instance.ActivateGameplayDice();
        }
    }

    void OnCurrentTurnChanged()
    {
        var currentPlayerData = FindObjectsOfType<PlayerData>()
            .FirstOrDefault(p => p.Object.InputAuthority == CurrentTurnPlayer);

        if (currentPlayerData != null)
        {
            Debug.Log($"[턴 전환] 이제 {currentPlayerData.PlayerName} 의 턴입니다.");
        }

        var uiManager = FindObjectOfType<GameUIManager>();
        if (uiManager != null)
        {
            uiManager.RefreshTurnUI();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_RequestReroll()
    {
        var myData = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);
        if (myData != null)
        {
            myData.ResetAndReroll();
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