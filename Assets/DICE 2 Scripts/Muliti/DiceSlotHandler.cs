using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DiceSlotHandler : MonoBehaviour
{
    public int slotIndex; // 인스펙터에서 0~4로 각각 설정

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnSlotClicked);
    }

    void OnSlotClicked()
    {
        var myData = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);

        if (myData != null)
        {
            myData.ResetSlot(slotIndex);
        }
    }
}