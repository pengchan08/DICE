using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RoomManager : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;

    [Header("방 만들기 화면")]
    public InputField createPlayerNameInput;
    public InputField createRoomNameInput;
    public Text generatedRoomCodeText;
    public Button createButton;

    [Header("방 참가 화면")]
    public InputField joinPlayerNameInput;
    public InputField joinRoomCodeInput;
    public Button joinButton;

    [Header("플레이어 데이터")]
    public NetworkPrefabRef playerDataPrefab;

    [Header("화면 전환")]
    public GameObject mainMenuCanvas;      // 방 만들기/참가 화면이 들어있는 Canvas
    public GameObject waitingRoomCanvas;   // 아까 만든 WaitingRoomCanvas

    [Header("에러 메시지")]
    public Text errorMessageText;

    private Coroutine errorMessageCoroutine;

    void Start()
    {
        createButton.onClick.AddListener(OnCreateRoom);
        joinButton.onClick.AddListener(OnJoinRoom);
    }

    async void OnCreateRoom()
    {
        createButton.interactable = false;

        string roomCode = GenerateRoomCode();
        generatedRoomCodeText.text = "방 코드: " + roomCode;

        GameData.PlayerName = createPlayerNameInput.text;
        GameData.RoomCode = roomCode;
        GameData.RoomName = createRoomNameInput.text;
        GameData.IsHost = true;

        await ConnectToRoom(roomCode);
    }

    async void OnJoinRoom()
    {
        string roomCode = joinRoomCodeInput.text;
        if (string.IsNullOrEmpty(roomCode))
        {
            ShowErrorMessage("방 코드를 입력해주세요.");
            return;
        }

        joinButton.interactable = false;

        GameData.PlayerName = joinPlayerNameInput.text;
        GameData.RoomCode = roomCode;
        GameData.IsHost = false;

        await ConnectToRoom(roomCode);
    }

    string GenerateRoomCode()
    {
        return Random.Range(1000, 9999).ToString();
    }

    async System.Threading.Tasks.Task ConnectToRoom(string roomCode)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = false;
        _runner.AddCallbacks(this);

        var startArgs = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = roomCode,
        };

        if (GameData.IsHost)
        {
            startArgs.SessionProperties = new Dictionary<string, SessionProperty>()
            {
                { "RoomName", GameData.RoomName },
                { "RoomExists", true } // 방장만 심는 "진짜 방입니다" 표시
            };
        }

        var result = await _runner.StartGame(startArgs);

        if (result.Ok)
        {
            // 참가자인 경우, 방이 실제로 존재했었는지 확인
            if (!GameData.IsHost)
            {
                bool roomExists = _runner.SessionInfo.Properties.TryGetValue("RoomExists", out var existsProp)
                                   && (bool)existsProp;

                if (!roomExists)
                {
                    ShowErrorMessage("해당 방이 존재하지 않습니다.");
                    await _runner.Shutdown(destroyGameObject: false);
                    Destroy(_runner);
                    _runner = null;
                    ResetMenuButtons();
                    return; // 대기실로 안 넘어가고 여기서 중단
                }
            }

            Debug.Log($"[디버그] 연결된 세션 이름: {_runner.SessionInfo.Name}, 현재 세션 인원: {_runner.SessionInfo.PlayerCount}");
            Debug.Log($"연결 성공! 방 코드: {roomCode}, 이름: {GameData.PlayerName}, 방장 여부: {GameData.IsHost}");
            mainMenuCanvas.SetActive(false);
            waitingRoomCanvas.SetActive(true);
        }
        else
        {
            ShowErrorMessage("연결에 실패했습니다. 다시 시도해주세요.");
            Debug.LogError("연결 실패: " + result.ShutdownReason);
            ResetMenuButtons();
        }
    }

    public async System.Threading.Tasks.Task LeaveRoom()
    {
        if (_runner != null)
        {
            await _runner.Shutdown(destroyGameObject: false);
            Destroy(_runner);
            _runner = null; // 완전히 정리해서, 다음 연결 때 새로 만들도록
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("플레이어 입장: " + player);

        if (player == runner.LocalPlayer)
        {
            runner.Spawn(playerDataPrefab, Vector3.zero, Quaternion.identity, player);
        }
    }

    public void ResetMenuButtons()
    {
        createButton.interactable = true;
        joinButton.interactable = true;
    }

    void ShowErrorMessage(string message)
    {
        errorMessageText.text = message;

        // 이전에 지우기 예약된 게 있으면 취소하고 새로 예약 (메시지가 연달아 떠도 안 꼬이게)
        if (errorMessageCoroutine != null)
        {
            StopCoroutine(errorMessageCoroutine);
        }
        errorMessageCoroutine = StartCoroutine(ClearErrorMessageAfterDelay());
    }

    IEnumerator ClearErrorMessageAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        errorMessageText.text = "";
        errorMessageCoroutine = null;
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}