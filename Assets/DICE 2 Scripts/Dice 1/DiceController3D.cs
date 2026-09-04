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
    }

    void Update()
    {
        diceVelocity = rb.velocity;
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
        var myData = FindMyPlayerData();
        if (myData == null) return;
        if (!myData.CanRollNow()) return;

        DoPhysicalRoll();
    }

    public void RollForFirstTurn()
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;
        if (!myData.CanRollForFirstTurn()) return;

        DoPhysicalRoll();
    }

    void DoPhysicalRoll()
    {
        EnsureCheckZone(); // 추가: 굴리기 직전에 한 번 더 확인

        if (checkZone == null)
        {
            Debug.LogError("DiceCheckZone3D를 찾을 수 없습니다. 씬에 체크존 오브젝트가 있는지, 활성화되어 있는지 확인해주세요.");
            return;
        }

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