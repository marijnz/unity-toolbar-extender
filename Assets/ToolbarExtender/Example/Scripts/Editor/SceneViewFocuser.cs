using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SceneViewFocuser
{
    static bool m_enabled;

    public static bool Enabled
    {
        get { return m_enabled; }
        set
        {
            m_enabled = value;
            EditorPrefs.SetBool("SceneViewFocuser", value);
        }
    }

    static SceneViewFocuser()
    {
        m_enabled = EditorPrefs.GetBool("SceneViewFocuser", false);
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.pauseStateChanged += OnPauseChanged;

        ToolbarExtension.RightToolbar.Add(OnToolbar);
    }

    private static void OnPauseChanged(PauseState obj)
    {
        if (Enabled && obj == PauseState.Unpaused)
        {
            // Not sure why, but this must be delayed
            EditorApplication.delayCall += () => EditorWindow.FocusWindowIfItsOpen<SceneView>();
        }
    }

    private static void OnPlayModeChanged(PlayModeStateChange obj)
    {
        if (Enabled && obj == PlayModeStateChange.EnteredPlayMode)
        {
            EditorWindow.FocusWindowIfItsOpen<SceneView>();
        }
    }

    private static void OnToolbar()
    {
        var tex = (Texture)EditorGUIUtility.Load(@"UnityEditor.SceneView");

        GUI.changed = false;
        GUILayout.Toggle(m_enabled, new GUIContent(null, tex, "Focus SceneView when entering play mode"), "Command");
        if (GUI.changed)
        {
            Enabled = !Enabled;
        }
    }
}
