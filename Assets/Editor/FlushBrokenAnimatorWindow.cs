using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class FlushBrokenAnimatorWindow
{
    const string SessionKey = "HeroKnight.FlushedAnimatorWindow";

    static FlushBrokenAnimatorWindow()
    {
        EditorApplication.delayCall += FlushOnce;
    }

    static void FlushOnce()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;
        SessionState.SetBool(SessionKey, true);

        Type toolType = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            toolType = assembly.GetType("UnityEditor.Graphs.AnimatorControllerTool");
            if (toolType != null)
                break;
        }

        if (toolType == null)
            return;

        var windows = Resources.FindObjectsOfTypeAll(toolType);
        for (int i = 0; i < windows.Length; i++)
        {
            var window = windows[i] as EditorWindow;
            if (window != null)
                window.Close();
        }
    }
}
