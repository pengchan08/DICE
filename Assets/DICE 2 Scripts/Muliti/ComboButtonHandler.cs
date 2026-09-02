using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ComboButtonHandler : MonoBehaviour
{
    public int comboIndex;
    private Text buttonText;
    private string comboName;

    void Awake()
    {
        buttonText = GetComponentInChildren<Text>();
        comboName = buttonText.text;
    }

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnComboClicked);
    }

    void OnComboClicked()
    {
        var myData = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);

        if (myData != null)
        {
            myData.ApplyScore(comboIndex);
        }
    }

    public void Refresh(PlayerData myData, bool isMyTurn)
    {
        if (buttonText == null) return;

        if (myData.UsedCombos[comboIndex])
        {
            buttonText.text = comboName + " (사용됨)";
            GetComponent<Button>().interactable = false;
        }
        else
        {
            int previewScore = myData.GetComboScore(comboIndex);
            buttonText.text = $"{comboName} ({previewScore})";
            GetComponent<Button>().interactable = isMyTurn;
        }
    }
}