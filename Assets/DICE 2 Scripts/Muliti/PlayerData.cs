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

    [Networked] public int HeldCardType { get; set; } // -1: 없음, 0: 추가굴리기, 1: 주사위변경, 2: 점수증가, 3: 부분재굴림
    [Networked] public NetworkBool HasUsedCardThisTurn { get; set; }
    [Networked] public int BonusRolls { get; set; } // 능력 카드로 얻은 추가 굴리기 횟수 (턴 종료 시 초기화)
    [Networked] public int PendingScoreBonus { get; set; }
    private const int ScoreBonusAmount = 20;

    [Networked] public NetworkString<_64> ActionLog { get; set; } // 마지막 행동 메시지 (조합 선택 / 카드 사용)

    public const int MaxRollsPerTurn = 3;
    private bool hasLoggedOnce = false;

    private static readonly string[] ComboNames = new string[]
    {
        "Ones", "Twos", "Threes", "Fours", "Fives", "Sixes",
        "Choice", "Four of a Kind", "Full House", "Small Straight", "Big Straight", "Yacht"
    };

    private static readonly string[] CardNames = new string[]
    {
        "추가 굴리기", "주사위 변경", "점수 증가", "부분 재굴림"
    };

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            PlayerName = GameData.PlayerName;
            IsHost = GameData.IsHost;
            IsReady = false;

            for (int i = 0; i < 5; i++)
            {
                DiceSlots.Set(i, 0);
                HeldDice.Set(i, false);
            }
            RollCount = 0;
            TotalScore = 0;

            // TODO: 4종 카드가 모두 구현되면 Random.Range(0, 4)로 교체
            HeldCardType = 0; // 지금은 "추가 굴리기" 카드로 고정 지급 (프로토타입 검증용)
            HasUsedCardThisTurn = false;
            BonusRolls = 0;
            PendingScoreBonus = 0;
            ActionLog = "";
        }
    }

    void Update()
    {
        if (!hasLoggedOnce && !string.IsNullOrEmpty(PlayerName.ToString()))
        {
            hasLoggedOnce = true;
        }
    }

    public void ToggleReady()
    {
        if (Object.HasStateAuthority) IsReady = !IsReady;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_StartGame()
    {
        var waitingRoomManager = FindObjectOfType<WaitingRoomManager>();
        if (waitingRoomManager != null) waitingRoomManager.OnGameStarted();
    }

    public void PromoteToHost()
    {
        if (Object.HasStateAuthority) IsHost = true;
    }

    public bool CanRollForFirstTurn()
    {
        if (!Object.HasStateAuthority) return false;
        return StartRoll == 0;
    }

    public void SetStartRoll(int result)
    {
        if (!Object.HasStateAuthority) return;
        if (StartRoll != 0) return;
        StartRoll = result;
        StartRollVersion++;
    }

    public void ResetStartRollOnly()
    {
        if (!Object.HasStateAuthority) return;
        StartRoll = 0;
    }

    // 능력 카드로 늘어난 굴리기 횟수를 포함한, 이번 턴의 실제 최대 굴리기 횟수
    public int GetMaxRollsThisTurn()
    {
        return MaxRollsPerTurn + BonusRolls;
    }

    public bool CanRollNow()
    {
        if (!Object.HasStateAuthority) return false;
        var gameState = FindObjectOfType<GameStateManager>();
        if (gameState == null || gameState.CurrentTurnPlayer != Object.InputAuthority) return false;
        if (RollCount >= GetMaxRollsThisTurn()) return false;
        return true;
    }

    public bool CanRollThisTurn()
    {
        if (!Object.HasStateAuthority) return false;
        var gameState = FindObjectOfType<GameStateManager>();
        if (gameState == null || gameState.CurrentTurnPlayer != Object.InputAuthority) return false;
        return RollCount < GetMaxRollsThisTurn();
    }

    public void IncrementRollCount()
    {
        if (!Object.HasStateAuthority) return;
        RollCount++;
    }

    public bool CanUseAbilityCard()
    {
        if (!Object.HasStateAuthority) return false;
        var gameState = FindObjectOfType<GameStateManager>();
        if (gameState == null || gameState.CurrentTurnPlayer != Object.InputAuthority) return false;
        if (HeldCardType == -1) return false;
        if (HasUsedCardThisTurn) return false;
        return true;
    }

    public void UseAbilityCard()
    {
        if (!CanUseAbilityCard()) return;

        int usedCardType = HeldCardType;

        switch (usedCardType)
        {
            case 0: // 추가 굴리기
                BonusRolls++;
                break;

            case 2: // 조합 점수 증가
                PendingScoreBonus += ScoreBonusAmount;
                break;

            // case 1: // 주사위 변경 - 추후 구현
            // case 3: // 부분 재굴림 - 추후 구현

            default:
                return; // 아직 구현되지 않은 카드는 사용 취소 (소모하지 않음)
        }

        HeldCardType = -1; // 카드 소모 (게임당 1장이므로 재지급 없음)
        HasUsedCardThisTurn = true;

        string cardName = (usedCardType >= 0 && usedCardType < CardNames.Length) ? CardNames[usedCardType] : "?";
        ActionLog = $"{PlayerName} : {cardName} 카드 사용";
    }

    public void SetDiceResult(int diceIndex, int result)
    {
        if (!Object.HasStateAuthority) return;
        if (diceIndex < 0 || diceIndex >= 5) return;
        DiceSlots.Set(diceIndex, result);
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
        return Mathf.Min(sum, 30);
    }

    public int GetFourOfAKindScore()
    {
        var count = new int[7];
        for (int i = 0; i < 5; i++) count[DiceSlots[i]]++;

        for (int i = 1; i <= 6; i++)
        {
            if (count[i] >= 4) return Mathf.Min(i * 4, 24);
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
            return 25;
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
        if (UsedCombos[comboIndex]) return;

        var gameState = FindObjectOfType<GameStateManager>();
        if (gameState == null || gameState.CurrentTurnPlayer != Object.InputAuthority) return;

        int score = GetComboScore(comboIndex);

        bool bonusApplied = false;
        if (PendingScoreBonus > 0)
        {
            score += PendingScoreBonus;
            PendingScoreBonus = 0;
            bonusApplied = true;
        }

        TotalScore += score;
        UsedCombos.Set(comboIndex, true);

        string comboName = (comboIndex >= 0 && comboIndex < ComboNames.Length) ? ComboNames[comboIndex] : "?";
        ActionLog = bonusApplied
            ? $"{PlayerName} : {comboName} 선택 ({score}점, 카드 보너스 포함)"
            : $"{PlayerName} : {comboName} 선택 ({score}점)";

        for (int i = 0; i < 5; i++)
        {
            DiceSlots.Set(i, 0);
            HeldDice.Set(i, false);
        }
        RollCount = 0;
        BonusRolls = 0;
        HasUsedCardThisTurn = false;

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
}