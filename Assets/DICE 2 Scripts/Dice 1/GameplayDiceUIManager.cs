using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class GameplayDiceUIManager : MonoBehaviour
{
    [Header("내 턴 UI 루트 (내 턴에만 활성화)")]
    public GameObject selfBoardPanel;

    [Header("주사위 슬롯 5개")]
    public Text[] diceValueTexts = new Text[5];
    public Button[] diceSlotButtons = new Button[5];
    public Image[] slotBackgrounds = new Image[5];

    [Header("색상")]
    public Color normalColor = Color.white;
    public Color heldColor = Color.yellow;

    [Header("굴리기")]
    public Button rollButton;
    public Text rollCountText;

    private int[] lastDiceValues = new int[5] { -1, -1, -1, -1, -1 };
    void Start()
    {
        for (int i = 0; i < 5; i++)
        {
            int index = i;
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

        if (selfBoardPanel != null) selfBoardPanel.SetActive(isMyTurn);
        if (!isMyTurn) return;

        bool isDiceRolling = DiceGroupController3D.Instance != null && DiceGroupController3D.Instance.IsAnyDiceRolling;
        int maxRollsThisTurn = myData.GetMaxRollsThisTurn();

        for (int i = 0; i < 5; i++)
        {
            int value = myData.DiceSlots[i];
            bool isHeld = myData.HeldDice[i];

            diceValueTexts[i].text = value == 0 ? "-" : value.ToString();

            if (value != lastDiceValues[i] && value != 0) StartCoroutine(PunchScale(diceValueTexts[i].transform));
            lastDiceValues[i] = value;

            slotBackgrounds[i].color = isHeld ? heldColor : normalColor;

            bool canToggle = isMyTurn && !isDiceRolling && value != 0;
            diceSlotButtons[i].interactable = canToggle;
        }

        bool canRoll = isMyTurn && !isDiceRolling
                       && myData.RollCount < maxRollsThisTurn
                       && myData.PendingCardAction == -1;

        rollButton.interactable = canRoll;

        rollCountText.text = $"남은 굴리기 : {maxRollsThisTurn - myData.RollCount}회";
    }

    IEnumerator PunchScale(Transform t)
    {
        Vector3 original = Vector3.one;
        t.localScale = original * 1.4f;
        float duration = 0.15f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(original * 1.4f, original, elapsed / duration);
            yield return null;
        }
        t.localScale = original;
    }

    void OnSlotClicked(int index)
    {
        var myData = FindMyPlayerData();
        if (myData.PendingCardAction == 2) return;
        if (myData == null) return;

        if (myData.PendingCardAction == 1 || myData.PendingCardAction == 3)
        {
            myData.SelectDiceForCardAction(index);
            return;
        }

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