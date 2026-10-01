using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TutorialProgress
{
    private const string Key = "TutorialCompleted";

    public static bool IsCompleted => PlayerPrefs.GetInt(Key, 0) == 1;

    public static void Complete()
    {
        PlayerPrefs.SetInt(Key, 1);
        PlayerPrefs.Save();
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Reset Tutorial")]
    static void ResetTutorial() => PlayerPrefs.DeleteKey(Key);
#endif
}
