using UnityEngine;
using System.Linq;

public class DiceCheckZone3D : MonoBehaviour
{
    [Header("연결")]
    public DiceController3D diceController;

    private Vector3 diceVelocity;
    private bool hasScored = false;
    private bool canCheck = false;
    private bool hasStartedMoving = false;

    private const float velocityThreshold = 0.05f;
    private const float requiredStillTime = 0.3f;
    private float stillTimer = 0f;
    private bool hasLoggedTriggerOnce = false;

    void Start()
    {
        diceController = FindObjectOfType<DiceController3D>();
    }

    void FixedUpdate()
    {
        if (diceController == null) return;

        diceVelocity = diceController.diceVelocity;

        bool isMoving = diceVelocity.magnitude > velocityThreshold
                         || diceController.diceAngularVelocity.magnitude > velocityThreshold;

        if (isMoving)
        {
            hasStartedMoving = true;
            hasScored = false;
            stillTimer = 0f;
        }
        else
        {
            stillTimer += Time.fixedDeltaTime;
        }
    }

    void OnTriggerStay(Collider col)
    {
        if (!hasLoggedTriggerOnce) // 추가: 이번 굴림에서 딱 한 번만 찍음
        {
            Debug.Log($"[진단] {gameObject.name}가 {col.gameObject.name}과 접촉함");
            hasLoggedTriggerOnce = true;
        }

        if (!canCheck) return;
        if (!hasStartedMoving) return;

        if (stillTimer >= requiredStillTime && !hasScored)
        {
            int number = 0;
            switch (col.gameObject.name)
            {
                case "Side1": number = 6; break;
                case "Side2": number = 5; break;
                case "Side3": number = 4; break;
                case "Side4": number = 3; break;
                case "Side5": number = 2; break;
                case "Side6": number = 1; break;
            }

            if (number != 0)
            {
                hasScored = true;
                canCheck = false;

                var myData = FindObjectsOfType<PlayerData>()
                    .Where(p => p != null && p.Object != null && p.Object.IsValid)
                    .FirstOrDefault(p => p.Object.HasStateAuthority);

                if (myData == null) return;

                var gameState = FindObjectOfType<GameStateManager>();
                bool isFirstTurnPhase = gameState == null || !gameState.FirstTurnDecided;

                if (isFirstTurnPhase)
                {
                    myData.SetStartRoll(number);
                }
                else
                {
                    DiceGroupController3D.Instance.OnDieLanded(diceController.diceIndex, number);
                }
            }
        }
    }

    public void EnableCheck()
    {
        canCheck = true;
        hasStartedMoving = false;
        hasScored = false;
        stillTimer = 0f;
        hasLoggedTriggerOnce = false;
    }
}