using UnityEngine;
using System.Linq;

public class DiceController3D : MonoBehaviour
{
    private Rigidbody rb;
    public Vector3 diceVelocity;
    public Vector3 diceAngularVelocity;

    public bool IsRolling { get; private set; }

    [Header("Sound")]
    public AudioClip diceRollClip;
    private AudioSource audioSource;

    private DiceCheckZone3D checkZone;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        diceVelocity = rb.velocity;
        diceAngularVelocity = rb.angularVelocity;
    }

    void EnsureCheckZone()
    {
        if (checkZone == null)
        {
            checkZone = FindObjectOfType<DiceCheckZone3D>();
        }
    }

    public void RollDice()
    {
        if (IsRolling) return;

        var myData = FindMyPlayerData();
        if (myData == null) return;
        if (!myData.CanRollNow()) return;

        DoPhysicalRoll();
    }

    public void RollForFirstTurn()
    {
        if (IsRolling)
        {
            return;
        }

        var myData = FindMyPlayerData();
        if (myData == null)
        {
            return;
        }

        if (!myData.CanRollForFirstTurn())
        {
            return;
        }

        DoPhysicalRoll();
    }

    void DoPhysicalRoll()
    {
        EnsureCheckZone();

        if (checkZone == null)
        {
            return;
        }

        IsRolling = true;

        if (audioSource != null && diceRollClip != null)
        {
            audioSource.PlayOneShot(diceRollClip);
        }

        float dirX = Random.Range(0, 500);
        float dirY = Random.Range(0, 500);
        float dirZ = Random.Range(0, 500);

        transform.position = new Vector3(0, 2, 0);
        transform.rotation = Quaternion.identity;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(Vector3.up * 600);
        rb.AddTorque(dirX, dirY, dirZ);

        checkZone.EnableCheck();
    }

    public void NotifyRollFinished()
    {
        IsRolling = false;
    }

    PlayerData FindMyPlayerData()
    {
        return FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);
    }
}