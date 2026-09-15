using UnityEngine;
using UnityEngine.UI;
using System.Linq;

// 인스펙터에서 Button + 자식 Text 하나로 구성된 카드 버튼 오브젝트에 붙여서 사용
public class AbilityCardButtonHandler : MonoBehaviour
{
    private Button button;
    private Text buttonText;

    void Awake()
    {
        button = GetComponent<Button>();
        buttonText = GetComponentInChildren<Text>();
    }

    void Start()
    {
        button.onClick.AddListener(OnCardClicked);
    }

    void Update()
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;

        Refresh(myData);
    }

    void OnCardClicked()
    {
        var myData = FindMyPlayerData();
        if (myData == null) return;

        myData.UseAbilityCard();
    }

    void Refresh(PlayerData myData)
    {
        if (myData.HeldCardType == -1)
        {
            if (buttonText != null) buttonText.text = "카드 없음";
            button.interactable = false;
            return;
        }

        if (buttonText != null) buttonText.text = GetCardName(myData.HeldCardType);
        button.interactable = myData.CanUseAbilityCard();
    }

    string GetCardName(int cardType)
    {
        switch (cardType)
        {
            case 0: return "추가 굴리기";
            case 1: return "주사위 변경";
            case 2: return "점수 증가";
            case 3: return "부분 재굴림";
            default: return "?";
        }
    }

    PlayerData FindMyPlayerData()
    {
        return FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);
    }
}