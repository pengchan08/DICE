using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class DiceController : MonoBehaviour
{
    private Rigidbody rb;
    public Vector3 diceVelocity;

    [Header("Sound")]
    public AudioClip diceRollClip;
    private AudioSource audioSource;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }
    
    void Update()
    {
        diceVelocity = rb.velocity;
    }

    public void RollDice()
    {
        if (GameManager.Instance.AreAllslotsFilled())
        {
            GameManager.Instance.ShowFullSlotMessage();
            return;
        }
        
        if (!GameManager.Instance.CanRollDice() || GameManager.Instance.isRolling) return;
        
        audioSource.PlayOneShot(diceRollClip);
        
        GameManager.Instance.diceNumber = 0;
        GameManager.Instance.isValueStored = false;
        GameManager.Instance.isRolling = true;

        float dirX = Random.Range(0, 500);
        float dirY = Random.Range(0, 500);
        float dirZ = Random.Range(0, 500);

        transform.position = new Vector3(0, 2, 0);
        transform.rotation = Quaternion.identity;
        rb.AddForce(Vector3.up * 600);
        rb.AddTorque(dirX, dirY, dirZ);
        
        FindObjectOfType<DiceCheckZone>().EnableCheck();
        
        GameManager.Instance.IncrementRollCount();
    }
}
