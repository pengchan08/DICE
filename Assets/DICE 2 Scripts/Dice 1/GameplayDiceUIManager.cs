using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameplayDiceUIManager : MonoBehaviour
{
    [Header("주사위 슬롯 5개")]
    public Text[] diceValueTexts = new Text[5];   // 각 슬롯의 숫자 표시
    public Button[] diceSlotButtons = new Button[5]; // 각 슬롯 클릭 = Hold 토글
    public Image[] slotBackgrounds = new Image[5];   // Hold 시 색 바뀔 배경

    [Header("색상")]
    public Color normalColor = Color.white;
    public Color heldColor = Color.yellow;

    [Header("굴리기")]
    public Button rollButton;
    public Text rollCountText;

    void Start()
    {
        for (int i = 0; i < 5; i++)
        {
            int index = i; // 클로저 캡처 주의
            diceSlotButtons[i].onClick.AddListener(() => OnSlotClicked(index));
        }

        rollButton.onClick.AddListener(OnRollClicked);
    }

    void Update()
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;

        var gameState = FindObjectOfType<GameStateManager>();
        bool isMyTurn = gameState != null && gameState.FirstTurnDecided
                        && gameState.CurrentTurnPlayer == myData.Object.InputAuthority;

        for (int i = 0; i < 5; i++)
        {
            int value = myData.DiceSlots[i];
            bool isHeld = myData.HeldDice[i];

            diceValueTexts[i].text = value == 0 ? "-" : value.ToString();
            slotBackgrounds[i].color = isHeld ? heldColor : normalColor;

            // 슬롯 클릭(Hold) 가능 조건: 내 턴 + 값이 있음(한 번 이상 굴림) + 아직 3회 안 씀
            bool canToggle = isMyTurn && value != 0 && myData.RollCount < PlayerData.MaxRollsPerTurn;
            diceSlotButtons[i].interactable = canToggle;
        }

        // 굴리기 버튼: 내 턴 + 아직 3회 안 씀
        bool canRoll = isMyTurn && myData.RollCount < PlayerData.MaxRollsPerTurn;
        rollButton.interactable = canRoll;

        rollCountText.text = $"남은 굴리기 : {PlayerData.MaxRollsPerTurn - myData.RollCount}회";
    }

    void OnSlotClicked(int index)
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;

        myData.ToggleHold(index);
    }

    void OnRollClicked()
    {
        DiceGroupController3D.Instance.RollNonHeldDice();
    }

    PlayerData FindMyPlayerData()
    {
        return FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);
    }
}