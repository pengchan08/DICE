using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiceCheckZone : MonoBehaviour
{
    private Vector3 diceVelocity;
    private DiceController _diceController;
    private bool hasScored = false;
    private bool canCheck = false;
    
    void Start()
    {
        _diceController = FindObjectOfType<DiceController>();
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
        
        if (diceVelocity == Vector3.zero && !GameManager.Instance.isValueStored && !hasScored)
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
                GameManager.Instance.diceNumber = number;
                GameManager.Instance.SaveToSlot(number);
                GameManager.Instance.isValueStored = true;
                hasScored = true;
                canCheck = false;
                
                GameManager.Instance.isRolling = false;
            }
        }
    }
    
    public void EnableCheck()
    {
        canCheck = true;
    }
}