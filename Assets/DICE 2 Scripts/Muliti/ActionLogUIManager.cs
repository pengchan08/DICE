using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ActionLogUIManager : MonoBehaviour
{
    [Header("행동 로그 텍스트")]
    public Text myActionLogText;
    public Text opponentActionLogText;

    void Update()
    {
        var players = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .ToList();

        var myData = players.FirstOrDefault(p => p.Object.HasStateAuthority);
        var opponentData = players.FirstOrDefault(p => !p.Object.HasStateAuthority);

        if (myData != null && myActionLogText != null)
        {
            myActionLogText.text = myData.ActionLog.ToString();
        }

        if (opponentData != null && opponentActionLogText != null)
        {
            opponentActionLogText.text = opponentData.ActionLog.ToString();
        }
    }
}