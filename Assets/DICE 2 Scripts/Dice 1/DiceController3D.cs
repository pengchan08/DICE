using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;

public class DiceController3D : NetworkBehaviour
{
    [Networked] public int diceIndex { get; set; } = -1; // -1: 선공 결정용, 0~4: 게임플레이용

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

    [Header("주사위 프리팹")]
    public NetworkPrefabRef[] dicePrefabs = new NetworkPrefabRef[5];
    private DiceController3D[] spawnedDice = new DiceController3D[5];

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
        BuildFaceDirections();
    }

    public override void Spawned()
    {
        Debug.Log($"[진단] Spawned 호출됨. diceIndex={diceIndex}, HasStateAuthority={Object.HasStateAuthority}");

        if (diceIndex == -1 && DiceGroupController3D.Instance != null)
        {
            DiceGroupController3D.Instance.RegisterFirstTurnDice(this);
        }

        if (diceIndex == -1 && Object.HasStateAuthority)
        {
            Debug.Log("[진단] 조건 통과 - RollForFirstTurn 호출");
            RollForFirstTurn();
        }
    }

    public void SpawnGameplayDice(NetworkRunner runner)
    {
        Vector3[] positions = new Vector3[]
        {
            new Vector3(-3, 1.5f, -2.5f),
            new Vector3(-1.5f, 1.5f, -2.5f),
            new Vector3(0, 1.5f, -2.5f),
            new Vector3(1.5f, 1.5f, -2.5f),
            new Vector3(3, 1.5f, -2.5f),
        };

        for (int i = 0; i < 5; i++)
        {
            var obj = runner.Spawn(dicePrefabs[i], positions[i], Quaternion.identity);
            spawnedDice[i] = obj.GetComponent<DiceController3D>();
            spawnedDice[i].diceIndex = i;
        }
    }

    void BuildFaceDirections()
    {
        faceLocalDirections.Clear();
        for (int i = 1; i <= 6; i++)
        {
            var child = transform.Find($"Side{i}");
            if (child == null) continue;
            faceLocalDirections[i] = child.localPosition.normalized;
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

            if (dot > bestDot) { bestDot = dot; bestValue = pair.Key; }
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
            myData.SetStartRoll(number);
        else
            DiceGroupController3D.Instance.OnDieLanded(diceIndex, number);
    }

    public void RollForFirstTurn()
    {
        Debug.Log("[진단] RollForFirstTurn 진입");
        if (IsRolling)
        {
            Debug.Log("[진단] 취소: IsRolling=true");
            return;
        }
        var myData = FindMyPlayerData();
        if (myData == null)
        {
            Debug.Log("[진단] 취소: myData=null");
            return;
        }
        if (!myData.CanRollForFirstTurn())
        {
            Debug.Log($"[진단] 취소: CanRollForFirstTurn=false (StartRoll={myData.StartRoll})");
            return;
        }
        Debug.Log("[진단] 통과 - DoPhysicalRoll 호출");
        DoPhysicalRoll();
    }

    public void RollGameplay()
    {
        if (IsRolling) return;
        DoPhysicalRoll();
        StartCoroutine(RollTimeoutFailsafe());
    }

    public void RequestAndRoll()
    {
        Debug.Log($"[진단] RequestAndRoll 호출됨. 현재 HasStateAuthority={Object.HasStateAuthority}");
        if (Object.HasStateAuthority)
        {
            RollForFirstTurn();
        }
        else
        {
            Debug.Log("[진단] 권한 요청 시작");
            Object.RequestStateAuthority();
            StartCoroutine(WaitForAuthorityThenRoll());
        }
    }

    IEnumerator WaitForAuthorityThenRoll()
    {
        float timeout = 3f;
        while (!Object.HasStateAuthority && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        Debug.Log($"[진단] 대기 종료. HasStateAuthority={Object.HasStateAuthority}, 남은시간={timeout:F2}");

        if (Object.HasStateAuthority)
        {
            RollForFirstTurn();
        }
        else
        {
            Debug.Log("[진단] 권한 획득 실패 - 타임아웃");
        }
    }

    IEnumerator RollTimeoutFailsafe()
    {
        yield return new WaitForSeconds(8f);
        if (IsRolling)
        {
            IsRolling = false;
            hasResolvedThisRoll = true;
            int number = GetTopFaceValue();
            var myData = FindMyPlayerData();
            if (myData != null)
            {
                var gameState = FindObjectOfType<GameStateManager>();
                bool isFirstTurnPhase = gameState == null || !gameState.FirstTurnDecided;
                if (isFirstTurnPhase) myData.SetStartRoll(number);
                else DiceGroupController3D.Instance.OnDieLanded(diceIndex, number);
            }
        }
    }

    void DoPhysicalRoll()
    {
        if (rb == null)
        {
            Debug.Log("[진단] DoPhysicalRoll 취소: rb=null");
            return;
        }
        if (!Object.HasStateAuthority)
        {
            Debug.Log("[진단] DoPhysicalRoll 취소: HasStateAuthority=false");
            return;
        }

        Debug.Log("[진단] DoPhysicalRoll 실행 - 힘 가함");

        IsRolling = true;
        hasStartedMoving = false;
        hasResolvedThisRoll = false;
        stillTimer = 0f;

        if (audioSource != null && diceRollClip != null)
            audioSource.PlayOneShot(diceRollClip);

        float dirX = Random.Range(0, 200);
        float dirY = Random.Range(0, 200);
        float dirZ = Random.Range(0, 200);

        transform.position = initialSpawnPos;
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