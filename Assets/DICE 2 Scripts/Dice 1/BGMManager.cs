using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("BGM 클립")]
    public AudioClip mainMenuBGM;
    public AudioClip waitingRoomBGM;
    public AudioClip gameBGM;
    public AudioClip resultBGM;

    [Header("페이드 시간")]
    public float fadeTime = 0.8f;

    private AudioSource audioSource;
    private AudioClip currentClip;
    private SoundSettings.BGMType activeType = SoundSettings.BGMType.MainMenu;
    private float fade = 1f;
    private Coroutine fadeRoutine;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
    }

    void Update()
    {
        audioSource.volume = fade * SoundSettings.EffectiveBGM(activeType);
    }

    public void Play(AudioClip clip)
    {
        if (clip == currentClip) return;
        currentClip = clip;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeToClip(clip, GetTypeOf(clip)));
    }

    SoundSettings.BGMType GetTypeOf(AudioClip clip)
    {
        if (clip == waitingRoomBGM) return SoundSettings.BGMType.WaitingRoom;
        if (clip == gameBGM) return SoundSettings.BGMType.Game;
        if (clip == resultBGM) return SoundSettings.BGMType.Result;
        return SoundSettings.BGMType.MainMenu;
    }

    IEnumerator FadeToClip(AudioClip clip, SoundSettings.BGMType type)
    {
        float start = fade;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            fade = Mathf.Lerp(start, 0f, t / fadeTime);
            yield return null;
        }
        fade = 0f;

        audioSource.clip = clip;
        activeType = type;
        if (clip != null) audioSource.Play();
        else audioSource.Stop();

        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            fade = Mathf.Lerp(0f, 1f, t / fadeTime);
            yield return null;
        }
        fade = 1f;
        fadeRoutine = null;
    }
}
