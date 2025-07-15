using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

public sealed class DebugInfoBehaviour : MonoBehaviour
{
    private Rect _rect = new Rect(20, 20, 0, 0);

    public Font font;

    private void OnGUI()
    {
        var title = string.Format("{0} ({1})", Application.productName, Application.version);

        GUI.skin.window.font = font;
        GUI.skin.label.font = font;
        GUI.skin.label.fontSize = 16;

        GUI.skin.label.wordWrap = false;
        _rect = GUILayout.Window(0, _rect, OnWindow, title, GUILayout.MinWidth(GUI.skin.label.CalcSize(new GUIContent(title)).x + 25));
    }

    private void OnWindow(int id)
    {
        GUILayout.Label("Unity: " + Application.unityVersion);
        GUILayout.Label("Platform: " + Application.platform);
        if (MonoVersion != null) GUILayout.Label("Mono: " + MonoVersion);

        GUI.DragWindow(new Rect(0, 0, float.MaxValue, 20));
    }

    [CanBeNull]
    private static readonly string MonoVersion;

    static DebugInfoBehaviour()
    {
        var monoRuntimeType = Type.GetType("Mono.Runtime");
        if (monoRuntimeType != null)
        {
            var getDisplayNameMethod = monoRuntimeType.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if (getDisplayNameMethod != null)
            {
                MonoVersion = (string)getDisplayNameMethod.Invoke(null, null);
            }
            else
            {
                MonoVersion = "<unknown>";
            }
        }
    }
}
