using UnityEngine;
using System.Linq;

public class DiceGroupController3D : MonoBehaviour
{
    public static DiceGroupController3D Instance;

    public DiceController3D[] diceControllers = new DiceController3D[5];

    [Header("게임플레이 주사위 오브젝트")]
    public GameObject[] gameplayDiceObjects = new GameObject[5];

    [Header("선공 결정용 주사위")]
    public GameObject firstTurnDiceObject;

    private int pendingCount = 0;

    void Awake()
    {
        Instance = this;
    }

    public void ActivateGameplayDice()
    {
        foreach (var dice in gameplayDiceObjects)
        {
            if (dice != null) dice.SetActive(true);
        }

        if (firstTurnDiceObject != null)
        {
            firstTurnDiceObject.SetActive(false);
        }
    }

    public void RollNonHeldDice()
    {
        if (pendingCount > 0)
        {
            Debug.LogWarning("[진단] 이전 굴림이 아직 끝나지 않음 - 재굴림 무시");
            return;
        }

        var myData = FindMyPlayerData();
        if (myData == null) return;
        if (!myData.CanRollThisTurn()) return;

        pendingCount = 0;
        bool rolledAny = false;

        for (int i = 0; i < 5; i++)
        {
            bool isHeld = myData.HeldDice[i];
            if (!isHeld)
            {
                pendingCount++;
                diceControllers[i].diceIndex = i;
                diceControllers[i].RollGameplay(); // DiceController3D의 함수를 "호출"하는 건 맞음
                rolledAny = true;
            }
        }

        if (!rolledAny) return;

        myData.IncrementRollCount();
    }

    public void OnDieLanded(int diceIndex, int number)
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;

        myData.SetDiceResult(diceIndex, number);

        pendingCount--;
        if (pendingCount <= 0)
        {
            Debug.Log("[게임 주사위] 이번 굴림 전체 착지 완료");
        }
    }

    public void ForceResolveDie(int diceIndex)
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;

        int fallbackValue = Random.Range(1, 7);
        myData.SetDiceResult(diceIndex, fallbackValue);
        Debug.LogWarning($"[주사위 {diceIndex}] 강제 확정: {fallbackValue}");

        pendingCount--;
    }

    PlayerData FindMyPlayerData()
    {
        return FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);
    }
}