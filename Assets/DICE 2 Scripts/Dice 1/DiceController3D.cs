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

    private const float velocityThreshold = 0.02f;
    private const float requiredStillTime = 0.6f;
    private float stillTimer = 0f;
    private bool hasStartedMoving = false;
    private bool hasResolvedThisRoll = false;

    private Vector3 initialSpawnPos;
    private Dictionary<int, Vector3> faceLocalDirections = new Dictionary<int, Vector3>();

    private float rollStartTime = 0f;
    private const float minRollDuration = 0.5f;

    private int rollVersion = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        BuildFaceDirections();
    }

    public override void Spawned()
    {
        initialSpawnPos = transform.position;

        if (diceIndex == -1 && DiceGroupController3D.Instance != null)
        {
            DiceGroupController3D.Instance.RegisterFirstTurnDice(this);
        }
        else if (diceIndex >= 0 && diceIndex < 5 && DiceGroupController3D.Instance != null)
        {
            DiceGroupController3D.Instance.RegisterGameplayDice(diceIndex, this); // 추가
        }

        if (diceIndex == -1 && Object.HasStateAuthority)
        {
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

    void DoPhysicalRoll()
    {
        if (rb == null) return;
        if (!Object.HasStateAuthority) return;

        rollVersion++;
        IsRolling = true;
        hasStartedMoving = false;
        hasResolvedThisRoll = false;
        stillTimer = 0f;
        rollStartTime = Time.time;

        if (audioSource != null && diceRollClip != null)
            audioSource.PlayOneShot(diceRollClip);

        float dirX = Random.Range(200, 500);
        float dirY = Random.Range(200, 500);
        float dirZ = Random.Range(200, 500);

        if (Random.value > 0.5f) dirX = -dirX;
        if (Random.value > 0.5f) dirY = -dirY;
        if (Random.value > 0.5f) dirZ = -dirZ;

        transform.position = initialSpawnPos;
        transform.rotation = Random.rotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(Vector3.up * 400);
        rb.AddTorque(dirX, dirY, dirZ);
    }

    void Update()
    {
        diceVelocity = rb.velocity;
        diceAngularVelocity = rb.angularVelocity;

        if (!IsRolling) return;
        if (Time.time - rollStartTime < minRollDuration) return;

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

    public void RollGameplay()
    {
        if (IsRolling) return;
        DoPhysicalRoll();
        int myVersion = rollVersion;
        StartCoroutine(RollTimeoutFailsafe(myVersion));
    }

    public void RequestAndRoll()
    {
        if (Object.HasStateAuthority)
        {
            RollForFirstTurn();
        }
        else
        {
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

        if (Object.HasStateAuthority)
        {
            yield return new WaitForFixedUpdate();
            RollForFirstTurn();
        }
    }
    public void RequestAndRollGameplay()
    {
        if (Object.HasStateAuthority)
        {
            RollGameplay();
        }
        else
        {
            Object.RequestStateAuthority();
            StartCoroutine(WaitForAuthorityThenRollGameplay());
        }
    }

    IEnumerator WaitForAuthorityThenRollGameplay()
    {
        float timeout = 3f;
        while (!Object.HasStateAuthority && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (Object.HasStateAuthority)
        {
            yield return new WaitForFixedUpdate(); // 권한 안정화 대기
            RollGameplay();
        }
    }

    IEnumerator RollTimeoutFailsafe(int version)
    {
        yield return new WaitForSeconds(8f);

        if (version != rollVersion) yield break;

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

    PlayerData FindMyPlayerData()
    {
        return FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);
    }
}