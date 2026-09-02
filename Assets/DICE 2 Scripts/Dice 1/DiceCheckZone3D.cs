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

                if (myData != null)
                {
                    myData.RollDice(number); // 실제 결정된 숫자를 전달
                }
            }
        }
    }

    public void EnableCheck()
    {
        canCheck = true;
    }
}