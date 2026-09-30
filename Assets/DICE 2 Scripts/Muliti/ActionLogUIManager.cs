using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class ActionLogUIManager : MonoBehaviour
{
    [Header("행동 로그 텍스트")]
    public Text myActionLogText;
    public Text opponentActionLogText;

    [Header("페이드용 CanvasGroup (각 텍스트에 부착)")]
    public CanvasGroup myLogGroup;
    public CanvasGroup opponentLogGroup;

    [Header("연출 시간")]
    public float fadeInTime = 0.2f;
    public float holdTime = 1.5f;
    public float fadeOutTime = 0.4f;

    private string lastMyLog = "";
    private string lastOpponentLog = "";
    private Coroutine myCoroutine;
    private Coroutine opponentCoroutine;

    void Start()
    {
        if (myLogGroup != null) myLogGroup.alpha = 0f;
        if (opponentLogGroup != null) opponentLogGroup.alpha = 0f;
    }

    void Update()
    {
        var players = FindObjectsOfType<PlayerData>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .ToList();

        var myData = players.FirstOrDefault(p => p.Object.HasStateAuthority);
        var opponentData = players.FirstOrDefault(p => !p.Object.HasStateAuthority);

        if (myData != null && myActionLogText != null)
        {
            string log = myData.ActionLog.ToString();
            if (!string.IsNullOrEmpty(log) && log != lastMyLog)
            {
                lastMyLog = log;
                myActionLogText.text = log;
                if (myLogGroup != null)
                {
                    if (myCoroutine != null) StopCoroutine(myCoroutine);
                    myCoroutine = StartCoroutine(PlayFade(myLogGroup));
                }
            }
        }

        if (opponentData != null && opponentActionLogText != null)
        {
            string log = opponentData.ActionLog.ToString();
            if (!string.IsNullOrEmpty(log) && log != lastOpponentLog)
            {
                lastOpponentLog = log;
                opponentActionLogText.text = log;
                if (opponentLogGroup != null)
                {
                    if (opponentCoroutine != null) StopCoroutine(opponentCoroutine);
                    opponentCoroutine = StartCoroutine(PlayFade(opponentLogGroup));
                }
            }
        }
    }

    IEnumerator PlayFade(CanvasGroup group)
    {
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            group.alpha = t / fadeInTime;
            yield return null;
        }
        group.alpha = 1f;

        yield return new WaitForSeconds(holdTime);

        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            group.alpha = 1f - (t / fadeOutTime);
            yield return null;
        }
        group.alpha = 0f;
    }
}