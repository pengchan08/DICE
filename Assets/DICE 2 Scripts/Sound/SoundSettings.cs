using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SoundSettings
{
    public enum BGMType { MainMenu, WaitingRoom, Game, Result }

    private const string MasterKey = "Vol_Master";
    private const string SfxKey = "Vol_SFX";
    private const string BgmKey = "Vol_BGM";

    public static float Master { get; private set; } = 1f;
    public static float SFX { get; private set; } = 1f;
    private static float[] bgm = { 1f, 1f, 1f, 1f };
    private static bool loaded;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;
        Master = PlayerPrefs.GetFloat(MasterKey, 1f);
        SFX = PlayerPrefs.GetFloat(SfxKey, 1f);
        for (int i = 0; i < bgm.Length; i++)
            bgm[i] = PlayerPrefs.GetFloat(BgmKey + i, 1f);
    }

    public static float GetBGM(BGMType t) => bgm[(int)t];

    public static void SetMaster(float v) { Master = v; PlayerPrefs.SetFloat(MasterKey, v); }
    public static void SetSFX(float v) { SFX = v; PlayerPrefs.SetFloat(SfxKey, v); }
    public static void SetBGM(BGMType t, float v) { bgm[(int)t] = v; PlayerPrefs.SetFloat(BgmKey + (int)t, v); }

    public static float EffectiveSFX => Master * SFX;
    public static float EffectiveBGM(BGMType t) => Master * bgm[(int)t];
}
