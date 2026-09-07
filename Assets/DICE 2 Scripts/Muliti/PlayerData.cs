using Fusion;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    [Networked] public NetworkString<_16> PlayerName { get; set; }
    [Networked] public NetworkBool IsReady { get; set; }
    [Networked] public NetworkBool IsHost { get; set; }
    [Networked] public int StartRoll { get; set; }
    [Networked] public int StartRollVersion { get; set; }

    [Networked, Capacity(5)] public NetworkArray<int> DiceSlots => default;
    [Networked, Capacity(5)] public NetworkArray<NetworkBool> HeldDice => default;
    [Networked] public int RollCount { get; set; }
    [Networked] public int TotalScore { get; set; }
    [Networked, Capacity(12)] public NetworkArray<NetworkBool> UsedCombos => default;

    public const int MaxRollsPerTurn = 3;
    private bool hasLoggedOnce = false;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            PlayerName = GameData.PlayerName;
            IsHost = GameData.IsHost;
            IsReady = false;

            // 추가: 게임 데이터 초기화
            for (int i = 0; i < 5; i++)
            {
                DiceSlots.Set(i, 0);
            }
            RollCount = 0;
            TotalScore = 0;
        }
    }

    void Update()
    {
        if (!hasLoggedOnce && !string.IsNullOrEmpty(PlayerName.ToString()))
        {
            Debug.Log($"[PlayerData] 이름: {PlayerName}, 방장: {IsHost}, StateAuthority 여부: {Object.HasStateAuthority}");
            hasLoggedOnce = true;
        }
    }

    public void ToggleReady()
    {
        if (Object.HasStateAuthority)
        {
            IsReady = !IsReady;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_StartGame()
    {
        Debug.Log("모든 클라이언트에서 게임 시작 신호 받음!");
        var waitingRoomManager = FindObjectOfType<WaitingRoomManager>();
        if (waitingRoomManager != null)
        {
            waitingRoomManager.OnGameStarted();
        }
    }

    public void PromoteToHost()
    {
        if (Object.HasStateAuthority)
        {
            IsHost = true;
        }
    }

    public bool CanRollForFirstTurn()
    {
        if (!Object.HasStateAuthority)
        {
            return false;
        }
        return StartRoll == 0;
    }

    public void SetStartRoll(int result)
    {
        if (!Object.HasStateAuthority) return;
        if (StartRoll != 0) return;
        StartRoll = result;
        StartRollVersion++;
    }

    void TriggerPhysicalReroll()
    {
        var diceController = FindObjectOfType<DiceController3D>();
        if (diceController != null)
        {
            diceController.RollForFirstTurn();
        }
    }

    public void ResetAndReroll()
    {
        if (Object.HasStateAuthority)
        {
            StartRoll = 0;
            Invoke(nameof(TriggerPhysicalReroll), 0.5f);
        }
    }

    public bool CanRollNow()
    {
        if (!Object.HasStateAuthority) return false;

        var gameState = FindObjectOfType<GameStateManager>();
        if (gameState == null || gameState.CurrentTurnPlayer != Object.InputAuthority) return false;

        if (RollCount >= 8) return false;
        if (AreAllSlotsFilled()) return false;

        return true;
    }

    public void RollDice(int result)
    {
        if (!CanRollNow()) return; // 안전장치: 여기서도 한 번 더 확인

        SaveToSlot(result);
        RollCount++;

        Debug.Log($"[주사위 굴림] {PlayerName}: {result} (남은 횟수: {8 - RollCount})");
    }

    bool AreAllSlotsFilled()
    {
        for (int i = 0; i < 5; i++)
        {
            if (DiceSlots[i] == 0) return false;
        }
        return true;
    }

    void SaveToSlot(int number)
    {
        for (int i = 0; i < 5; i++)
        {
            if (DiceSlots[i] == 0)
            {
                DiceSlots.Set(i, number);
                break;
            }
        }
    }

    public void ResetSlot(int index)
    {
        if (!Object.HasStateAuthority) return;

        var gameState = FindObjectOfType<GameStateManager>();
        if (gameState == null || gameState.CurrentTurnPlayer != Object.InputAuthority) return;

        if (index < 0 || index >= 5) return;
        DiceSlots.Set(index, 0);
    }

    public int CalculateNumberScore(int targetNumber)
    {
        int score = 0;
        for (int i = 0; i < 5; i++)
        {
            if (DiceSlots[i] == targetNumber) score += targetNumber;
        }
        return score;
    }

    public int GetChoiceScore()
    {
        int sum = 0;
        for (int i = 0; i < 5; i++) sum += DiceSlots[i];
        return Mathf.Min(sum, 30); // 최대 30점 제한
    }

    public int GetFourOfAKindScore()
    {
        var count = new int[7];
        for (int i = 0; i < 5; i++) count[DiceSlots[i]]++;

        for (int i = 1; i <= 6; i++)
        {
            if (count[i] >= 4) return Mathf.Min(i * 4, 24); // 최대 24점 제한
        }
        return 0;
    }

    public int GetFullHouseScore()
    {
        var count = new int[7];
        for (int i = 0; i < 5; i++) count[DiceSlots[i]]++;

        bool hasThree = false, hasTwo = false;
        for (int i = 1; i <= 6; i++)
        {
            if (count[i] == 3) hasThree = true;
            if (count[i] == 2) hasTwo = true;
        }

        if (hasThree && hasTwo)
        {
            int sum = 0;
            for (int i = 0; i < 5; i++) sum += DiceSlots[i];
            return sum;
        }
        return 0;
    }

    public int GetSmallStraightScore()
    {
        var present = new bool[7];
        for (int i = 0; i < 5; i++)
        {
            if (DiceSlots[i] > 0) present[DiceSlots[i]] = true;
        }

        // 1~4 연속, 2~5 연속, 3~6 연속 중 하나라도 있으면 인정 (4개 이상 이어짐)
        for (int start = 1; start <= 3; start++)
        {
            bool ok = true;
            for (int n = start; n < start + 4; n++)
            {
                if (!present[n]) { ok = false; break; }
            }
            if (ok) return 15;
        }
        return 0;
    }

    public int GetBigStraightScore()
    {
        var present = new bool[7];
        for (int i = 0; i < 5; i++)
        {
            if (DiceSlots[i] > 0) present[DiceSlots[i]] = true;
        }

        bool little = present[1] && present[2] && present[3] && present[4] && present[5];
        bool big = present[2] && present[3] && present[4] && present[5] && present[6];

        return (little || big) ? 30 : 0;
    }

    public int GetYachtScore()
    {
        var count = new int[7];
        for (int i = 0; i < 5; i++) count[DiceSlots[i]]++;

        for (int i = 1; i <= 6; i++)
        {
            if (count[i] == 5) return 50;
        }
        return 0;
    }

    public int GetComboScore(int comboIndex)
    {
        switch (comboIndex)
        {
            case 0: return CalculateNumberScore(1);
            case 1: return CalculateNumberScore(2);
            case 2: return CalculateNumberScore(3);
            case 3: return CalculateNumberScore(4);
            case 4: return CalculateNumberScore(5);
            case 5: return CalculateNumberScore(6);
            case 6: return GetChoiceScore();
            case 7: return GetFourOfAKindScore();
            case 8: return GetFullHouseScore();
            case 9: return GetSmallStraightScore();
            case 10: return GetBigStraightScore();
            case 11: return GetYachtScore();
            default: return 0;
        }
    }

    public void ApplyScore(int comboIndex)
    {
        if (!Object.HasStateAuthority) return;
        if (UsedCombos[comboIndex]) return; // 이미 쓴 조합이면 무시

        var gameState = FindObjectOfType<GameStateManager>();
        if (gameState == null || gameState.CurrentTurnPlayer != Object.InputAuthority)
        {
            Debug.LogWarning("지금은 당신의 턴이 아닙니다.");
            return;
        }

        int score = GetComboScore(comboIndex);
        TotalScore += score;
        UsedCombos.Set(comboIndex, true);

        // 다음 턴을 위해 슬롯/굴리기 횟수 초기화
        for (int i = 0; i < 5; i++)
        {
            DiceSlots.Set(i, 0);
            HeldDice.Set(i, false);
        }
        RollCount = 0;

        Debug.Log($"[조합 선택] {PlayerName}: 조합 {comboIndex}번 선택, {score}점 획득 (총점: {TotalScore})");

        if (gameState != null)
        {
            gameState.RPC_EndTurn();
        }
    }

    public bool AreAllCombosUsed()
    {
        for (int i = 0; i < 12; i++)
        {
            if (!UsedCombos[i]) return false;
        }
        return true;
    }

    public void ToggleHold(int index)
    {
        if (!Object.HasStateAuthority) return;

        var gameState = FindObjectOfType<GameStateManager>();
        if (gameState == null || gameState.CurrentTurnPlayer != Object.InputAuthority) return;

        if (index < 0 || index >= 5) return;
        if (DiceSlots[index] == 0) return;
        if (RollCount == 0) return;

        HeldDice.Set(index, !HeldDice[index]);
    }

    public void SetDiceResult(int diceIndex, int result)
    {
        if (!Object.HasStateAuthority) return;
        if (diceIndex < 0 || diceIndex >= 5) return;

        DiceSlots.Set(diceIndex, result);
        Debug.Log($"[게임 주사위] {PlayerName}: 슬롯 {diceIndex} = {result}");
    }

    public void IncrementRollCount()
    {
        if (!Object.HasStateAuthority) return;
        RollCount++;
        Debug.Log($"[턴 굴리기] {PlayerName}: {RollCount}/{MaxRollsPerTurn}회");
    }

    public bool CanRollThisTurn()
    {
        if (!Object.HasStateAuthority) return false;

        var gameState = FindObjectOfType<GameStateManager>();
        if (gameState == null || gameState.CurrentTurnPlayer != Object.InputAuthority) return false;

        return RollCount < MaxRollsPerTurn;
    }
}