using UnityEngine;
using System.Linq;
using Fusion;

public class DiceGroupController3D : MonoBehaviour
{
    public static DiceGroupController3D Instance;

    public DiceController3D[] diceControllers = new DiceController3D[5];

    [Header("게임플레이 주사위 오브젝트")]
    public GameObject[] gameplayDiceObjects = new GameObject[5];

    [Header("선공 결정용 주사위")]
    public GameObject firstTurnDiceObject;

    [Header("게임플레이 주사위 프리팹")]
    public NetworkPrefabRef gameplayDicePrefab;

    private int pendingCount = 0;
    private DiceController3D firstTurnDiceController;
    public DiceController3D FirstTurnDice => firstTurnDiceController;
    public bool IsAnyDiceRolling => pendingCount > 0;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnGameplayDice()
    {
        if (!GameData.IsHost) return;

        var runner = FindObjectOfType<NetworkRunner>();
        Vector3[] positions = new Vector3[]
        {
        new Vector3(-3, 1.5f, -2.5f),
        new Vector3(-1.5f, 1.5f, -2.5f),
        new Vector3(0, 1.5f, -2.5f),
        new Vector3(1.5f, 1.5f, -2.5f),
        new Vector3(3, 1.5f, -2.5f),
        };

        for (int i = 0; i < 5; i++)
        {
            int index = i; // 클로저 캡처 주의
            runner.Spawn(
                gameplayDicePrefab,
                positions[i],
                Quaternion.identity,
                onBeforeSpawned: (r, obj) =>
                {
                    var dc = obj.GetComponent<DiceController3D>();
                    dc.diceIndex = index;
                }
            );
        }
    }

    public void RegisterGameplayDice(int index, DiceController3D controller)
    {
        if (diceControllers == null || diceControllers.Length != 5)
            diceControllers = new DiceController3D[5];

        diceControllers[index] = controller;
    }

    public void ActivateGameplayDice()
    {
        SpawnGameplayDice();

        if (firstTurnDiceController != null && firstTurnDiceController.Object != null)
        {
            if (firstTurnDiceController.Object.HasStateAuthority)
            {
                var runner = FindObjectOfType<NetworkRunner>();
                runner.Despawn(firstTurnDiceController.Object);
            }
            firstTurnDiceController = null;
        }
    }

    public void RollNonHeldDice()
    {
        if (pendingCount > 0)
        {
            Debug.LogWarning($"[진단] 재굴림 무시됨. 현재 pendingCount={pendingCount}");
            return;
        }

        var myData = FindMyPlayerData();
        if (myData == null) return;
        if (!myData.CanRollThisTurn()) return;

        pendingCount = 0;
        bool rolledAny = false;

        Debug.Log("[진단] === 새 굴림 시작 ===");

        for (int i = 0; i < 5; i++)
        {
            bool isHeld = myData.HeldDice[i];
            Debug.Log($"[진단] 슬롯 {i}: Held={isHeld}");
            if (!isHeld)
            {
                pendingCount++;
                var dc = diceControllers[i];
                dc.RequestAndRollGameplay();
                rolledAny = true;
            }
        }

        Debug.Log($"[진단] 이번 굴림 대상 개수(pendingCount)={pendingCount}");

        if (!rolledAny) return;
        myData.IncrementRollCount();
    }

    public void OnDieLanded(int diceIndex, int number)
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;

        Debug.Log($"[진단] OnDieLanded 호출: diceIndex={diceIndex}, number={number}, 호출 전 pendingCount={pendingCount}");

        myData.SetDiceResult(diceIndex, number);
        pendingCount--;

        Debug.Log($"[진단] 호출 후 pendingCount={pendingCount}");
    }

    public void ForceResolveDie(int diceIndex)
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;

        int fallbackValue = Random.Range(1, 7);
        myData.SetDiceResult(diceIndex, fallbackValue);

        pendingCount--;
    }

    public void RegisterFirstTurnDice(DiceController3D controller)
    {
        firstTurnDiceController = controller;
    }

    PlayerData FindMyPlayerData()
    {
        return FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);
    }
}