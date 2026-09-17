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

    [Header("선공 결정용 주사위 프리팹 (다시하기 재스폰용)")]
    public NetworkPrefabRef firstTurnDicePrefab;
    private int pendingCount = 0;
    private DiceController3D firstTurnDiceController;
    public DiceController3D FirstTurnDice => firstTurnDiceController;
    public bool IsAnyDiceRolling
    {
        get
        {
            if (pendingCount > 0) return true;

            if (diceControllers != null)
            {
                foreach (var dc in diceControllers)
                {
                    if (dc != null && dc.IsRolling) return true;
                }
            }
            return false;
        }
    }

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
        if (pendingCount > 0) return;

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
                var dc = diceControllers[i];
                dc.RequestAndRollGameplay();
                rolledAny = true;
            }
        }

        if (!rolledAny) return;
        myData.IncrementRollCount();
    }

    public void RollSingleDieForCard(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= diceControllers.Length) return;

        var dc = diceControllers[slotIndex];
        if (dc == null) return;

        pendingCount++;
        dc.RequestAndRollGameplay();
    }

    public void OnDieLanded(int diceIndex, int number)
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;

        myData.SetDiceResult(diceIndex, number);
        pendingCount--;
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

    public void ResetForRematch()
    {
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner == null) return;

        for (int i = 0; i < diceControllers.Length; i++)
        {
            var dc = diceControllers[i];
            if (dc != null && dc.Object != null && dc.Object.IsValid)
            {
                if (GameData.IsHost)
                {
                    runner.Despawn(dc.Object);
                }
            }
            diceControllers[i] = null;
        }

        if (firstTurnDiceController != null && firstTurnDiceController.Object != null && firstTurnDiceController.Object.IsValid)
        {
            if (GameData.IsHost)
            {
                runner.Despawn(firstTurnDiceController.Object);
            }
            firstTurnDiceController = null;
        }

        if (GameData.IsHost)
        {
            var diceObj = runner.Spawn(
                firstTurnDicePrefab,
                new Vector3(0, 2, 0),
                Quaternion.identity,
                onBeforeSpawned: (r, obj) =>
                {
                    var dc = obj.GetComponent<DiceController3D>();
                    dc.diceIndex = -1;
                }
            );

            var diceController = diceObj.GetComponent<DiceController3D>();
            RegisterFirstTurnDice(diceController);
        }
    }

    PlayerData FindMyPlayerData()
    {
        return FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);
    }
}