using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnBannerUIManager : MonoBehaviour
{
    public CanvasGroup bannerGroup;
    public Text bannerText;

    [Header("연출 시간")]
    public float fadeInTime = 0.2f;
    public float holdTime = 0.8f;
    public float fadeOutTime = 0.3f;

    private Coroutine current;

    public void ShowTurnBanner(string playerName, bool isMyTurn)
    {
        bannerText.text = isMyTurn ? "내 턴" : $"{playerName}의 턴";

        gameObject.SetActive(true);

        if (current != null) StopCoroutine(current);
        current = StartCoroutine(PlayBanner());
    }

    IEnumerator PlayBanner()
    {
        bannerGroup.alpha = 0f;

        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            bannerGroup.alpha = t / fadeInTime;
            yield return null;
        }
        bannerGroup.alpha = 1f;

        yield return new WaitForSeconds(holdTime);

        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            bannerGroup.alpha = 1f - (t / fadeOutTime);
            yield return null;
        }
        bannerGroup.alpha = 0f;
        bannerGroup.gameObject.SetActive(false);
    }
}
