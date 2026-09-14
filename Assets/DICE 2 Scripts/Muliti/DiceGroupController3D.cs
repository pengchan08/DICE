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

    private int pendingCount = 0;
    private DiceController3D firstTurnDiceController;
    public DiceController3D FirstTurnDice => firstTurnDiceController;

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
                dc.diceIndex = i;

                if (!dc.Object.HasStateAuthority)
                {
                    dc.Object.RequestStateAuthority();
                }

                dc.RollGameplay();
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