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

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
    }

    public void Play(AudioClip clip)
    {
        if (clip == currentClip) return;
        currentClip = clip;
        StopAllCoroutines();
        StartCoroutine(FadeToClip(clip));
    }

    IEnumerator FadeToClip(AudioClip clip)
    {
        float startVolume = audioSource.volume;

        float t = 0f;
        while (t < fadeTime && audioSource.volume > 0f)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
            yield return null;
        }

        audioSource.clip = clip;
        if (clip != null) audioSource.Play();
        else audioSource.Stop();

        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, startVolume, t / fadeTime);
            yield return null;
        }
        audioSource.volume = startVolume;
    }
}
