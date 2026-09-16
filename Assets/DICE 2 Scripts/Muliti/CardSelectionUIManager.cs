using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class CardSelectionUIManager : MonoBehaviour
{
    [Header("선공 전용 - 카드 선택 버튼 2개")]
    public GameObject selectionPanel;
    public Button cardButtonA;
    public Text cardButtonAText;
    public Button cardButtonB;
    public Text cardButtonBText;

    [Header("후공 전용 - 대기 안내")]
    public GameObject waitingPanel;

    [Header("게임 화면 잠금 대상 (굴리기/슬롯/콤보 등이 들어있는 부모)")]
    public GameObject gameplayInteractionRoot;

    private static readonly string[] CardNames = new string[]
    {
        "추가 굴리기", "주사위 변경", "점수 증가", "부분 재굴림"
    };

    void Start()
    {
        cardButtonA.onClick.AddListener(() => OnCardChosen(true));
        cardButtonB.onClick.AddListener(() => OnCardChosen(false));
    }

    void Update()
    {
        var gameState = FindObjectOfType<GameStateManager>();
        if (gameState == null) return;

        var myData = FindMyPlayerData();
        if (myData == null) return;

        bool isPending = gameState.CardSelectionPending;
        bool isWinner = gameState.CurrentTurnPlayer == myData.Object.InputAuthority;

        if (selectionPanel != null) selectionPanel.SetActive(isPending && isWinner);
        if (waitingPanel != null) waitingPanel.SetActive(isPending && !isWinner);
        if (gameplayInteractionRoot != null) gameplayInteractionRoot.SetActive(!isPending);

        if (isPending && isWinner)
        {
            cardButtonAText.text = GetCardName(gameState.CardOptionA);
            cardButtonBText.text = GetCardName(gameState.CardOptionB);
        }
    }

    void OnCardChosen(bool isOptionA)
    {
        var gameState = FindObjectOfType<GameStateManager>();
        var myData = FindMyPlayerData();
        if (gameState == null || myData == null) return;

        int chosen = isOptionA ? gameState.CardOptionA : gameState.CardOptionB;
        gameState.RPC_RequestSelectCard(myData.Object.InputAuthority, chosen);
    }

    string GetCardName(int cardType)
    {
        return (cardType >= 0 && cardType < CardNames.Length) ? CardNames[cardType] : "?";
    }

    PlayerData FindMyPlayerData()
    {
        return FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);
    }
}