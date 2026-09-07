using UnityEngine;
using System.Collections;
using System.Linq;

public class DiceController3D : MonoBehaviour
{
    public int diceIndex = -1;

    private Rigidbody rb;
    public Vector3 diceVelocity;
    public Vector3 diceAngularVelocity;

    public bool IsRolling { get; private set; }

    [Header("Sound")]
    public AudioClip diceRollClip;
    private AudioSource audioSource;

    [Header("연결")]
    public DiceCheckZone3D checkZone;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        diceVelocity = rb.velocity;
        diceAngularVelocity = rb.angularVelocity;
    }

    public void RollForFirstTurn()
    {
        if (IsRolling) return;

        var myData = FindMyPlayerData();
        if (myData == null) return;
        if (!myData.CanRollForFirstTurn()) return;

        DoPhysicalRoll();
    }

    public void RollGameplay() // 여기 있어야 함
    {
        if (IsRolling) return;
        DoPhysicalRoll();
        StartCoroutine(RollTimeoutFailsafe()); // 여기 있어야 함
    }

    IEnumerator RollTimeoutFailsafe() // 여기 있어야 함
    {
        yield return new WaitForSeconds(5f);
        if (IsRolling)
        {
            Debug.LogWarning($"[주사위 {diceIndex}] 5초 내 착지 판정 실패 - 강제 재배치 후 재시도");
            transform.position = new Vector3(diceIndex * 1.5f - 3f, 1.5f, 0);
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            IsRolling = false;
            DiceGroupController3D.Instance.ForceResolveDie(diceIndex);
        }
    }

    void DoPhysicalRoll()
    {
        if (rb == null)
        {
            Debug.LogError($"[주사위 {diceIndex}] Rigidbody가 없습니다.");
            return;
        }

        if (checkZone == null)
        {
            Debug.LogError($"[주사위 {diceIndex}] checkZone이 연결되지 않았습니다.");
            return;
        }

        IsRolling = true;

        if (audioSource != null && diceRollClip != null)
        {
            audioSource.PlayOneShot(diceRollClip);
        }

        float dirX = Random.Range(0, 200);
        float dirY = Random.Range(0, 200);
        float dirZ = Random.Range(0, 200);

        Vector3 spawnPos = diceIndex >= 0
            ? new Vector3(diceIndex * 1.5f - 3f, 1.5f, 0)
            : new Vector3(0, 2, 0);

        transform.position = spawnPos;
        transform.rotation = Quaternion.identity;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(Vector3.up * 300);
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