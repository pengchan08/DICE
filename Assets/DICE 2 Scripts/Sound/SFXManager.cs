using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("효과음 클립")]
    public AudioClip buttonClick;
    public AudioClip cardUse;
    public AudioClip comboSelect;

    private AudioSource source;

    void Awake()
    {
        Instance = this;
        SoundSettings.Load();

        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    void Start()
    {
        foreach (var btn in FindObjectsOfType<Button>(true))
            btn.onClick.AddListener(PlayButton);
    }

    public void Play(AudioClip clip)
    {
        if (clip == null) return;
        source.PlayOneShot(clip, SoundSettings.EffectiveSFX);
    }

    public void PlayButton() => Play(buttonClick);
    public void PlayCard() => Play(cardUse);
    public void PlayCombo() => Play(comboSelect);
}
