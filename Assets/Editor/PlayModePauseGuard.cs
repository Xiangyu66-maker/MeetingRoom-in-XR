#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PlayModePauseGuard
{
    static PlayModePauseGuard()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode && state != PlayModeStateChange.EnteredPlayMode)
        {
            return;
        }

        ClearPauseState(state.ToString());

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.delayCall += () => ClearPauseState("EnteredPlayMode delayed check");
        }
    }

    private static void ClearPauseState(string source)
    {
        if (!EditorApplication.isPaused)
        {
            return;
        }

        EditorApplication.isPaused = false;
        Debug.Log($"Unity Editor pause state cleared during {source}.");
    }
}
#endif
