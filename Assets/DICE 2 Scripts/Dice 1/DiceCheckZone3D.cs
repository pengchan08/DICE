using UnityEngine;
using System.Linq;

public class DiceCheckZone3D : MonoBehaviour
{
    private Vector3 diceVelocity;
    private DiceController3D _diceController;
    private bool hasScored = false;
    private bool canCheck = false;

    void Start()
    {
        _diceController = FindObjectOfType<DiceController3D>();
    }

    void FixedUpdate()
    {
        if (_diceController == null)
        {
            _diceController = FindObjectOfType<DiceController3D>();
            if (_diceController == null) return; // 아직 못 찾았으면 이번 프레임은 건너뜀
        }

        diceVelocity = _diceController.diceVelocity;

        if (diceVelocity.magnitude > 0.1f)
        {
            hasScored = false;
        }
    }

    void OnTriggerStay(Collider col)
    {
        if (!canCheck) return;

        if (diceVelocity == Vector3.zero && !hasScored)
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
                    myData.SetStartRoll(number); // 선공 결정용: 슬롯에 안 들어감
                }
                else
                {
                    myData.RollDice(number); // 진짜 게임 굴리기: 슬롯에 저장됨
                }
            }
        }
    }

    public void EnableCheck()
    {
        canCheck = true;
    }
}