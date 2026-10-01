using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class TutorialManager : MonoBehaviour
{
    [Header("화면")]
    public GameObject mainMenuCanvas;
    public GameObject gameCanvas;
    public Button tutorialStartButton;

    [Header("안내 UI")]
    public GameObject tutorialPanel;
    public Text stepText;
    public Text guideText;
    public Button exitButton;
    public GameObject completePanel;
    public Button completeButton;

    [Header("네트워크")]
    public NetworkPrefabRef gameStateManagerPrefab;
    [Header("단계별 조작 제한 (각 대상에 CanvasGroup 추가)")]
    public CanvasGroup rollGroup;
    public CanvasGroup slotGroup;
    public CanvasGroup cardGroup;
    public CanvasGroup comboGroup;

    [Header("튜토리얼 중 숨길 UI (상대 점수 등)")]
    public GameObject[] hideInTutorial;

    [Header("타자기 효과")]
    public float typeInterval = 0.04f;
    public float stepStartDelay = 0.5f;

    private bool isTyping;
    private bool stepReady;
    private string currentLine;
    private Coroutine typingRoutine;

    private const string CompleteText = "잘했어요! 이제 진짜 대전을 즐겨보세요!";

    private static readonly string[] StepTexts =
    {
        "안녕하세요! Lucky Dice 2에 오신 걸 환영해요.\n먼저 굴리기 버튼을 눌러 주사위를 굴려볼까요?",
        "좋아요! 이제 마음에 드는 주사위 슬롯을 눌러 고정(Hold)해보세요.\n고정한 슬롯에 주사위는 다시 굴려도 바뀌지 않아요.",
        "굴리기를 한 번 더 눌러보세요.\n고정하지 않은 주사위만 다시 굴러가요!",
        "능력 카드를 하나 드렸어요!\n카드를 눌러 '추가 굴리기'를 써보세요. 굴릴 수 있는 횟수가 1회 늘어나요.",
        "마지막이에요! 지금 나온 주사위로 점수를 얻을 조합을 하나 골라보세요.",
    };

    private int step = -1;
    private bool running;
    private bool isFinalLine;

    void Start()
    {
        tutorialStartButton.onClick.AddListener(OnStartClicked);
        exitButton.onClick.AddListener(() => Finish(false));
        completeButton.onClick.AddListener(() => Finish(true));
        tutorialPanel.SetActive(false);
        completePanel.SetActive(false);
    }

    async void OnStartClicked()
    {
        if (running) return;

        var roomManager = FindObjectOfType<RoomManager>();
        if (roomManager == null) return;

        running = true;
        tutorialStartButton.interactable = false;
        bool ok = await roomManager.StartTutorialSession();
        tutorialStartButton.interactable = true;

        if (!ok) { running = false; return; }

        mainMenuCanvas.SetActive(false);
        gameCanvas.SetActive(true);
        if (BGMManager.Instance != null) BGMManager.Instance.Play(BGMManager.Instance.gameBGM);

        var runner = FindObjectOfType<NetworkRunner>();
        runner.Spawn(gameStateManagerPrefab, Vector3.zero, Quaternion.identity);

        foreach (var go in hideInTutorial) if (go != null) go.SetActive(false);

        completePanel.SetActive(false);
        tutorialPanel.SetActive(true);
        SetStep(0);
    }

    void Update()
    {
        if (!running || step < 0 || step >= StepTexts.Length) return;

        if (isTyping)
        {
            if (Input.GetMouseButtonDown(0)) SkipTyping();
            return;
        }
        if (!stepReady) return;

        var me = FindMe();
        if (me == null) return;

        bool rolling = DiceGroupController3D.Instance != null && DiceGroupController3D.Instance.IsAnyDiceRolling;
        if (rolling) return;

        if (IsStepDone(step, me)) SetStep(step + 1);
    }

    void SetStep(int s)
    {
        step = s;
        stepReady = false;

        SetGroup(rollGroup, false);
        SetGroup(slotGroup, false);
        SetGroup(cardGroup, false);
        SetGroup(comboGroup, false);

        bool done = s >= StepTexts.Length;
        isFinalLine = done;
        completePanel.SetActive(false);
        if (!done) stepText.text = $"{s + 1} / {StepTexts.Length}";

        currentLine = done ? CompleteText : StepTexts[s];

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeLine());
    }

    bool IsStepDone(int s, PlayerData me)
    {
        switch (s)
        {
            case 0: return me.RollCount >= 1 && AllDiceFilled(me);
            case 1: return AnyHeld(me);
            case 2: return me.RollCount >= 2 && AllDiceFilled(me);
            case 3: return me.HasUsedCardThisTurn;
            case 4: return AnyComboUsed(me);
        }
        return false;
    }

    bool AllDiceFilled(PlayerData me)
    {
        for (int i = 0; i < 5; i++) if (me.DiceSlots[i] == 0) return false;
        return true;
    }

    bool AnyHeld(PlayerData me)
    {
        for (int i = 0; i < 5; i++) if (me.HeldDice[i]) return true;
        return false;
    }

    bool AnyComboUsed(PlayerData me)
    {
        for (int i = 0; i < 12; i++) if (me.UsedCombos[i]) return true;
        return false;
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        guideText.text = "";
        yield return new WaitForSeconds(stepStartDelay);

        for (int i = 1; i <= currentLine.Length; i++)
        {
            guideText.text = currentLine.Substring(0, i);

            yield return new WaitForSeconds(typeInterval);
        }

        FinishTyping();
    }

    void SkipTyping()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        FinishTyping();
    }

    void FinishTyping()
    {
        typingRoutine = null;
        isTyping = false;
        guideText.text = currentLine;

        if (isFinalLine)
        {
            StartCoroutine(ShowCompletePanel());
            return;
        }
        OpenStep(step);
    }

    IEnumerator ShowCompletePanel()
    {
        yield return new WaitForSeconds(1f);
        completePanel.SetActive(true);
    }

    void OpenStep(int s)
    {
        SetGroup(rollGroup, s == 0 || s == 2);
        SetGroup(slotGroup, s == 1 || s == 2);
        SetGroup(cardGroup, s == 3);
        SetGroup(comboGroup, s == 4);

        if (s == 3)
        {
            var me = FindMe();
            if (me != null) me.AssignCard(0);
        }

        stepReady = true;
    }

    void SetGroup(CanvasGroup g, bool allow)
    {
        if (g == null) return;
        g.interactable = allow;
        g.blocksRaycasts = allow;
    }

    async void Finish(bool completed)
    {
        if (!running) return;

        StopAllCoroutines();
        isTyping = false;
        stepReady = false;
        running = false;
        step = -1;

        if (completed) { TutorialProgress.Complete(); Debug.Log("[튜토리얼] 완료 저장됨"); }
        GameData.IsTutorial = false;

        var roomManager = FindObjectOfType<RoomManager>();
        if (roomManager != null)
        {
            await roomManager.LeaveRoom();
            roomManager.ResetMenuButtons();
        }

        if (DiceGroupController3D.Instance != null) DiceGroupController3D.Instance.ClearLocalState();

        var firstTurnUI = FindObjectOfType<FirstTurnUIManager>(true);
        if (firstTurnUI != null) firstTurnUI.ShowTexts();

        SetGroup(rollGroup, true);
        SetGroup(slotGroup, true);
        SetGroup(cardGroup, true);
        SetGroup(comboGroup, true);
        foreach (var go in hideInTutorial) if (go != null) go.SetActive(true);

        tutorialPanel.SetActive(false);
        completePanel.SetActive(false);
        gameCanvas.SetActive(false);
        mainMenuCanvas.SetActive(true);

        if (BGMManager.Instance != null) BGMManager.Instance.Play(BGMManager.Instance.mainMenuBGM);
    }

    PlayerData FindMe()
    {
        return FindObjectsOfType<PlayerData>()
            .FirstOrDefault(p => p != null && p.Object != null && p.Object.IsValid && p.Object.HasStateAuthority);
    }
}
