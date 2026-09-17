using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DebugTestUIManager : MonoBehaviour
{
    public Button debugFillButton;

    void Start()
    {
#if UNITY_EDITOR
        if (debugFillButton != null)
        {
            debugFillButton.onClick.AddListener(OnDebugFillClicked);
        }
#else
        // 빌드에서는 버튼 자체를 숨김
        if (debugFillButton != null) debugFillButton.gameObject.SetActive(false);
#endif
    }

    void OnDebugFillClicked()
    {
#if UNITY_EDITOR
        var myData = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .FirstOrDefault(p => p.Object.HasStateAuthority);

        if (myData != null)
        {
            myData.DebugFillDiceForTest();
        }
#endif
    }
}