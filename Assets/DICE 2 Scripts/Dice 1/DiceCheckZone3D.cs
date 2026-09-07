using UnityEngine;
using System.Linq;

public class DiceCheckZone3D : MonoBehaviour
{
    private Vector3 diceVelocity;
    private DiceController3D _diceController;
    private bool hasScored = false;
    private bool canCheck = false;
    private bool hasStartedMoving = false;

    private const float velocityThreshold = 0.05f;
    private const float requiredStillTime = 0.3f;
    private float stillTimer = 0f;

    void Start()
    {
        _diceController = FindObjectOfType<DiceController3D>();
    }

    void FixedUpdate()
    {
        if (_diceController == null)
        {
            _diceController = FindObjectOfType<DiceController3D>();
            if (_diceController == null) return;
        }

        diceVelocity = _diceController.diceVelocity;

        bool isMoving = diceVelocity.magnitude > velocityThreshold
            || _diceController.diceAngularVelocity.magnitude > velocityThreshold;

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
        if (!canCheck) return;
        if (!hasStartedMoving) return;

        // if (Time.frameCount % 30 == 0)
        // {
        //     Debug.Log($"[진단] OnTriggerStay 진입: col={col.gameObject.name}, stillTimer={stillTimer:F2}, velocity={diceVelocity.magnitude:F3}");
        // }

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
                    myData.RollDice(number);
                }

                _diceController.NotifyRollFinished();
            }
        }
    }

    public void EnableCheck()
    {
        canCheck = true;
        hasStartedMoving = false;
        hasScored = false;
        stillTimer = 0f;
    }
}