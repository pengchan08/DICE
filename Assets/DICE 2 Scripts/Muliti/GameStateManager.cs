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

    [Networked, OnChangedRender(nameof(OnGameEndedChanged))]
    public NetworkBool GameEnded { get; set; }

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

        if (p1.StartRoll == 0 || p2.StartRoll == 0)
        {
            RerollInProgress = false;
            return;
        }

        if (p1.StartRoll == p2.StartRoll)
        {
            if (!RerollInProgress)
            {
                RerollInProgress = true;
                RPC_RequestReroll();
            }
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