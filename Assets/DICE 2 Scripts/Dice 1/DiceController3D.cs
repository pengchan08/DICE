using UnityEngine;
using System.Linq;

public class DiceController3D : MonoBehaviour
{
    private Rigidbody rb;
    public Vector3 diceVelocity;

    [Header("Sound")]
    public AudioClip diceRollClip;
    private AudioSource audioSource;

    private DiceCheckZone3D checkZone;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        checkZone = FindObjectOfType<DiceCheckZone3D>();
    }

    void Update()
    {
        diceVelocity = rb.velocity;
    }

    public void RollDice()
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;

        // 내 턴인지, 굴릴 수 있는 상태인지는 PlayerData가 판단
        if (!myData.CanRollNow()) return;

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

    PlayerData FindMyPlayerData()
    {
        return FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);
    }
}