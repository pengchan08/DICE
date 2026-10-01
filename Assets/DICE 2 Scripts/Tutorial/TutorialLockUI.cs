using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TutorialLockUI : MonoBehaviour
{
    [System.Serializable]
    public class LockSlot
    {
        public Button button;
        public Image lockImage;
        public UnityEvent onAllowed;

        [HideInInspector] public Vector2 originPos;
        [HideInInspector] public Color originColor;
        [HideInInspector] public Coroutine routine;
    }

    [Header("방 생성 / 방 참가)")]
    public LockSlot createSlot;
    public LockSlot joinSlot;

    [Header("연출")]
    public Color warnColor = Color.red;
    public float shakeDuration = 0.4f;
    public float shakeStrength = 8f;
    public int shakeCount = 3;

    void Awake()
    {
        Init(createSlot);
        Init(joinSlot);
    }

    void Init(LockSlot s)
    {
        var rt = (RectTransform)s.lockImage.transform;
        s.originPos = rt.anchoredPosition;
        s.originColor = s.lockImage.color;
        s.button.onClick.AddListener(() => OnClicked(s));
    }

    void OnEnable()
    {
        bool locked = !TutorialProgress.IsCompleted;
        ResetSlot(createSlot, locked);
        ResetSlot(joinSlot, locked);
    }

    void ResetSlot(LockSlot s, bool locked)
    {
        s.routine = null;
        ((RectTransform)s.lockImage.transform).anchoredPosition = s.originPos;
        s.lockImage.color = s.originColor;
        s.lockImage.gameObject.SetActive(locked);
    }

    void OnClicked(LockSlot s)
    {
        if (TutorialProgress.IsCompleted)
        {
            s.onAllowed.Invoke();
            return;
        }

        if (s.routine != null) StopCoroutine(s.routine);
        s.routine = StartCoroutine(Shake(s));
    }

    IEnumerator Shake(LockSlot s)
    {
        var rt = (RectTransform)s.lockImage.transform;
        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / shakeDuration);
            float offset = Mathf.Sin(p * shakeCount * Mathf.PI * 2f) * shakeStrength * (1f - p);

            rt.anchoredPosition = s.originPos + new Vector2(offset, 0f);
            s.lockImage.color = Color.Lerp(warnColor, s.originColor, p);
            yield return null;
        }

        rt.anchoredPosition = s.originPos;
        s.lockImage.color = s.originColor;
        s.routine = null;
    }
}
