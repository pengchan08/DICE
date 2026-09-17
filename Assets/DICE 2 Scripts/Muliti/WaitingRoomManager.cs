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
    public GameObject player1ReadyObject;
    public GameObject player1NormalObject;

    [Header("플레이어 2")]
    public Text player2NameText;
    public Text player2StatusText;
    public GameObject player2ReadyObject;
    public GameObject player2NormalObject;

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

    [Header("주사위 프리팹")]
    public NetworkPrefabRef firstTurnDicePrefab;

    private NetworkRunner _runner;
    private bool isCheckingPromotion = false;
    private bool hasGameStarted = false;

    void Start()
    {
        _runner = FindObjectOfType<NetworkRunner>();

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
        var players = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .OrderByDescending(p => (bool)p.IsHost)
            .ToList();

        bool anyHostPresent = players.Any(p => p.IsHost);
        var myData = players.FirstOrDefault(p => p.Object.HasStateAuthority);

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
            SetReadyVisual(player1ReadyObject, player1NormalObject, p.IsReady);
        }
        else
        {
            player1NameText.text = "-";
            player1StatusText.text = "";
            SetReadyVisual(player1ReadyObject, player1NormalObject, false);
        }

        if (players.Count >= 2)
        {
            var p = players[1];
            player2NameText.text = p.PlayerName.ToString() + (p.IsHost ? " (방장)" : " (참가자)");
            player2StatusText.text = p.IsReady ? "READY" : "WAITING";
            SetReadyVisual(player2ReadyObject, player2NormalObject, p.IsReady);
        }
        else
        {
            player2NameText.text = "-";
            player2StatusText.text = "";
            SetReadyVisual(player2ReadyObject, player2NormalObject, false);
        }

        bool allReady = players.Count == 2 && players.All(p => p.IsReady);
        startGameButton.gameObject.SetActive(GameData.IsHost);
        startGameButton.interactable = allReady;
    }

    void SetReadyVisual(GameObject readyObj, GameObject normalObj, bool isReady)
    {
        if (readyObj != null) readyObj.SetActive(isReady);
        if (normalObj != null) normalObj.SetActive(!isReady);
    }

    IEnumerator ConfirmAndPromote(PlayerData myData)
    {
        isCheckingPromotion = true;
        yield return new WaitForSeconds(1.5f);

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
        startGameButton.interactable = false;

        var myPlayerData = FindMyPlayerData();
        if (myPlayerData != null)
        {
            myPlayerData.RPC_StartGame();
        }
    }

    public void OnGameStarted()
    {
        if (hasGameStarted) return;
        hasGameStarted = true;

        gameObject.SetActive(false);
        gameCanvas.SetActive(true);

        if (GameData.IsHost)
        {
            var runner = FindObjectOfType<NetworkRunner>();

            var existing = FindObjectOfType<GameStateManager>();
            if (existing == null)
            {
                runner.Spawn(gameStateManagerPrefab, Vector3.zero, Quaternion.identity);
            }

            var diceObj = runner.Spawn(
                firstTurnDicePrefab,
                new Vector3(0, 2, 0),
                Quaternion.identity,
                onBeforeSpawned: (runner, obj) =>
                {
                    var dc = obj.GetComponent<DiceController3D>();
                    dc.diceIndex = -1;
                }
            );

            var diceController = diceObj.GetComponent<DiceController3D>();

            if (DiceGroupController3D.Instance != null)
            {
                DiceGroupController3D.Instance.RegisterFirstTurnDice(diceController);
            }
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