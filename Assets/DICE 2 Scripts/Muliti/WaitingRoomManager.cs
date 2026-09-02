using Fusion;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

public class WaitingRoomManager : MonoBehaviour
{
    [Header("방 정보")]
    public Text roomNameText;
    public Text roomCodeText;
    public Text playerCountText;

    [Header("플레이어 1")]
    public Text player1NameText;
    public Text player1StatusText;

    [Header("플레이어 2")]
    public Text player2NameText;
    public Text player2StatusText;

    [Header("버튼")]
    public Button readyButton;
    public Button startGameButton;
    public Button leaveRoomButton;

    [Header("화면 전환")]
    public GameObject mainMenuCanvas;

    [Header("게임 상태")]
    public NetworkPrefabRef gameStateManagerPrefab;

    [Header("게임 화면")]
    public GameObject gameCanvas;

    private NetworkRunner _runner;
    private bool isCheckingPromotion = false;

    void Start()
    {
        _runner = FindObjectOfType<NetworkRunner>();

        // 세션 속성에서 방 이름을 읽어옴 (방장이든 참가자든 동일하게 동작)
        string roomName = "-";
        if (_runner != null && _runner.SessionInfo.Properties.TryGetValue("RoomName", out var prop))
        {
            roomName = prop.PropertyValue.ToString();
        }

        roomNameText.text = "방 이름: " + roomName;
        roomCodeText.text = "방 코드: " + GameData.RoomCode;

        readyButton.onClick.AddListener(OnReadyClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);
        leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
    }

    void Update()
    {
        RefreshPlayerList();
    }

    void RefreshPlayerList()
    {
        // 씬에 있는 모든 PlayerData를 찾고, 방장이 먼저 오도록 정렬
        var players = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .OrderByDescending(p => (bool)p.IsHost)
            .ToList();

        bool anyHostPresent = players.Any(p => p.IsHost);
        var myData = players.FirstOrDefault(p => p.Object.HasStateAuthority);

        // 방장이 안 보이는 상황이면, 바로 승격하지 않고 잠깐 기다렸다가 재확인
        if (!anyHostPresent && myData != null && !isCheckingPromotion)
        {
            StartCoroutine(ConfirmAndPromote(myData));
        }

        playerCountText.text = $"{players.Count} / 2 PLAYERS";

        if (players.Count >= 1)
        {
            var p = players[0];
            player1NameText.text = p.PlayerName.ToString() + (p.IsHost ? " (방장)" : " (참가자)");
            player1StatusText.text = p.IsReady ? "READY" : "WAITING";
        }
        else
        {
            player1NameText.text = "-";
            player1StatusText.text = "";
        }

        if (players.Count >= 2)
        {
            var p = players[1];
            player2NameText.text = p.PlayerName.ToString() + (p.IsHost ? " (방장)" : " (참가자)");
            player2StatusText.text = p.IsReady ? "READY" : "WAITING";
        }
        else
        {
            player2NameText.text = "-";
            player2StatusText.text = "";
        }

        bool allReady = players.Count == 2 && players.All(p => p.IsReady);
        startGameButton.gameObject.SetActive(GameData.IsHost);
        startGameButton.interactable = allReady;
    }

    IEnumerator ConfirmAndPromote(PlayerData myData)
    {
        isCheckingPromotion = true;
        yield return new WaitForSeconds(1.5f); // 잠깐 기다리며 네트워크 동기화 시간을 줌

        // 기다린 후 다시 확인: 그 사이에 진짜 방장이 나타났으면 승격 취소
        var players = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .ToList();
        bool anyHostPresent = players.Any(p => p.IsHost);

        if (!anyHostPresent && myData != null && myData.Object != null && myData.Object.IsValid)
        {
            myData.PromoteToHost();
            GameData.IsHost = true;
            Debug.Log("방장이 없어서 자동 승격되었습니다.");
        }

        isCheckingPromotion = false;
    }

    void OnReadyClicked()
    {
        var myPlayerData = FindMyPlayerData();
        if (myPlayerData != null)
        {
            myPlayerData.ToggleReady();
        }
    }

    void OnStartGameClicked()
    {
        var myPlayerData = FindMyPlayerData();
        if (myPlayerData != null)
        {
            myPlayerData.RPC_StartGame();
        }
    }

    public void OnGameStarted()
    {
        gameObject.SetActive(false);
        gameCanvas.SetActive(true);

        if (GameData.IsHost)
        {
            var runner = FindObjectOfType<NetworkRunner>();
            runner.Spawn(gameStateManagerPrefab, Vector3.zero, Quaternion.identity);
        }

        var diceController = FindObjectOfType<DiceController3D>();
        if (diceController != null)
        {
            diceController.RollForFirstTurn();
        }
    }

    async void OnLeaveRoomClicked()
    {
        var roomManager = FindObjectOfType<RoomManager>();
        if (roomManager != null)
        {
            await roomManager.LeaveRoom();
            roomManager.ResetMenuButtons();
        }

        mainMenuCanvas.SetActive(true);
        gameObject.SetActive(false);

        Debug.Log("방을 나갔습니다.");
    }

    PlayerData FindMyPlayerData()
    {
        var players = FindObjectsOfType<PlayerData>();
        foreach (var p in players)
        {
            if (p.Object.HasStateAuthority) return p;
        }
        return null;
    }
}