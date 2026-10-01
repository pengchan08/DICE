using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUIManager : MonoBehaviour
{
    public GameObject settingsPanel;
    public Button openButton;

    [Header("마스터 / 효과음")]
    public Slider masterSlider;
    public Text masterText;
    public Slider sfxSlider;
    public Text sfxText;

    [Header("화면별 BGM (순서: 메인 메뉴, 대기실, 게임, 결과)")]
    public Slider[] bgmSliders = new Slider[4];
    public Text[] bgmTexts = new Text[4];

    void Start()
    {
        SoundSettings.Load();

        Bind(masterSlider, masterText, SoundSettings.Master, SoundSettings.SetMaster);
        Bind(sfxSlider, sfxText, SoundSettings.SFX, SoundSettings.SetSFX);

        for (int i = 0; i < bgmSliders.Length; i++)
        {
            var type = (SoundSettings.BGMType)i;
            Bind(bgmSliders[i], bgmTexts[i], SoundSettings.GetBGM(type),
                v => SoundSettings.SetBGM(type, v));
        }

        openButton.onClick.AddListener(Toggle);
        settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Toggle();
    }

    void Bind(Slider slider, Text label, float initial, System.Action<float> onChange)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(initial);
        SetLabel(label, initial);

        slider.onValueChanged.AddListener(v =>
        {
            onChange(v);
            SetLabel(label, v);
        });
    }

    void SetLabel(Text label, float v)
    {
        if (label != null) label.text = $"{Mathf.RoundToInt(v * 100)}%";
    }

    void Toggle()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
}
