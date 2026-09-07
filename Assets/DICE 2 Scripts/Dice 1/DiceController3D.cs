using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DiceController3D : MonoBehaviour
{
    public int diceIndex = -1; // -1: 선공 결정용, 0~4: 게임플레이용

    private Rigidbody rb;
    public Vector3 diceVelocity;
    public Vector3 diceAngularVelocity;

    public bool IsRolling { get; private set; }

    [Header("Sound")]
    public AudioClip diceRollClip;
    private AudioSource audioSource;

    [Header("판정 설정")]
    [Tooltip("체크 후 값이 반대로 나오면 이 옵션을 켜보세요 (예: 1이 나와야 하는데 6이 나옴)")]
    public bool useOppositeFace = false; // 필요하면 인스펙터에서 켜서 반대값 사용

    private const float velocityThreshold = 0.15f;
    private const float requiredStillTime = 0.2f;
    private float stillTimer = 0f;
    private bool hasStartedMoving = false;
    private bool hasResolvedThisRoll = false;

    private Vector3 initialSpawnPos;
    private Dictionary<int, Vector3> faceLocalDirections = new Dictionary<int, Vector3>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        initialSpawnPos = transform.position;

        BuildFaceDirections(); // 추가: Side1~6 자식의 위치로 방향 자동 계산
    }

    void BuildFaceDirections()
    {
        faceLocalDirections.Clear();

        for (int i = 1; i <= 6; i++)
        {
            var child = transform.Find($"Side{i}");
            if (child == null)
            {
                Debug.LogWarning($"[주사위 {diceIndex}] Side{i} 자식 오브젝트를 못 찾았습니다.");
                continue;
            }

            // 자식의 로컬 위치를 정규화 → "이 면이 로컬 좌표계에서 어느 방향인지"
            Vector3 localDir = child.localPosition.normalized;
            faceLocalDirections[i] = localDir;
        }
    }

    void Update()
    {
        diceVelocity = rb.velocity;
        diceAngularVelocity = rb.angularVelocity;

        if (!IsRolling) return;

        bool isMoving = diceVelocity.magnitude > velocityThreshold
                         || diceAngularVelocity.magnitude > velocityThreshold;

        if (isMoving)
        {
            hasStartedMoving = true;
            stillTimer = 0f;
        }
        else
        {
            stillTimer += Time.deltaTime;
        }

        // 실제로 굴러간 적 있고, 충분히 멈춰있었으면 판정
        if (hasStartedMoving && !hasResolvedThisRoll && stillTimer >= requiredStillTime)
        {
            ResolveTopFace();
        }
    }

    int GetTopFaceValue()
    {
        int bestValue = 1;
        float bestDot = -2f;

        foreach (var pair in faceLocalDirections)
        {
            Vector3 worldDir = transform.TransformDirection(pair.Value);
            float dot = Vector3.Dot(worldDir, Vector3.up);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestValue = pair.Key;
            }
        }

        return useOppositeFace ? (7 - bestValue) : bestValue;
    }

    void ResolveTopFace()
    {
        hasResolvedThisRoll = true;
        IsRolling = false;

        int number = GetTopFaceValue();

        var myData = FindMyPlayerData();
        if (myData == null) return;

        var gameState = FindObjectOfType<GameStateManager>();
        bool isFirstTurnPhase = gameState == null || !gameState.FirstTurnDecided;

        if (isFirstTurnPhase)
        {
            myData.SetStartRoll(number);
        }
        else
        {
            DiceGroupController3D.Instance.OnDieLanded(diceIndex, number);
        }
    }

    public void RollForFirstTurn()
    {
        if (IsRolling) return;

        var myData = FindMyPlayerData();
        if (myData == null) return;
        if (!myData.CanRollForFirstTurn()) return;

        DoPhysicalRoll();
    }

    public void RollGameplay()
    {
        if (IsRolling) return;
        DoPhysicalRoll();
        StartCoroutine(RollTimeoutFailsafe());
    }

    IEnumerator RollTimeoutFailsafe()
    {
        yield return new WaitForSeconds(5f);
        if (IsRolling)
        {
            IsRolling = false;
            transform.position = initialSpawnPos;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            DiceGroupController3D.Instance.ForceResolveDie(diceIndex);
        }
    }

    void DoPhysicalRoll()
    {
        if (rb == null) return;

        IsRolling = true;
        hasStartedMoving = false;
        hasResolvedThisRoll = false;
        stillTimer = 0f;

        if (audioSource != null && diceRollClip != null)
        {
            audioSource.PlayOneShot(diceRollClip);
        }

        float dirX = Random.Range(0, 200);
        float dirY = Random.Range(0, 200);
        float dirZ = Random.Range(0, 200);

        transform.position = initialSpawnPos; // 변경: 에디터에 배치된 원래 위치 사용
        transform.rotation = Quaternion.identity;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(Vector3.up * 300);
        rb.AddTorque(dirX, dirY, dirZ);
    }

    PlayerData FindMyPlayerData()
    {
        return FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);
    }
}