using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class CardTargetSelectionUIManager : MonoBehaviour
{
    [Header("대상 선택 안내 배너")]
    public GameObject selectionBanner;
    public Text selectionBannerText;

    [Header("값 선택 패널 (주사위 변경 전용, 1~6 버튼)")]
    public GameObject valuePickerPanel;
    public Button[] valueButtons = new Button[6]; // 인스펙터에서 1~6 순서로 연결

    [Header("취소 버튼")]
    public Button cancelButton;

    void Start()
    {
        for (int i = 0; i < valueButtons.Length; i++)
        {
            int value = i + 1;
            if (valueButtons[i] != null)
                valueButtons[i].onClick.AddListener(() => OnValueChosen(value));
        }

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
    }

    void Update()
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;

        bool selectingSlot = myData.PendingCardAction == 1 || myData.PendingCardAction == 3;
        bool pickingValue = myData.PendingCardAction == 2;

        if (selectionBanner != null) selectionBanner.SetActive(selectingSlot);
        if (selectionBannerText != null && selectingSlot)
        {
            selectionBannerText.text = myData.PendingCardAction == 1
                ? "값을 바꿀 주사위를 선택하세요"
                : "다시 굴릴 주사위를 선택하세요";
        }

        if (valuePickerPanel != null) valuePickerPanel.SetActive(pickingValue);
        if (cancelButton != null) cancelButton.gameObject.SetActive(selectingSlot || pickingValue);
    }

    void OnValueChosen(int value)
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;
        myData.ConfirmDiceChange(value);
    }

    void OnCancelClicked()
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;
        myData.CancelCardAction();
    }

    PlayerData FindMyPlayerData()
    {
        return FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);
    }
}