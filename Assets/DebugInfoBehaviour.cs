#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
#define UNITY_5_0_OR_NEWER
#endif

using System;
using System.Reflection;
using UnityEngine;

public sealed class DebugInfoBehaviour : MonoBehaviour
{
    private Rect _rect = new Rect(20, 20, 0, 0);

    public Font font;

    private void OnGUI()
    {
#if UNITY_5_0_OR_NEWER
        var title = string.Format("{0} ({1})", Application.productName, Application.version);
#else
        var title = "TestGame";
#endif

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
        if (FrameworkDescription != null) GUILayout.Label("Runtime: " + FrameworkDescription);
        else if (MonoVersion != null) GUILayout.Label("Mono: " + MonoVersion);

        GUI.DragWindow(new Rect(0, 0, float.MaxValue, 20));
    }

    private static readonly string MonoVersion;
    private static readonly string FrameworkDescription;

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

        var runtimeInformationType = Type.GetType("System.Runtime.InteropServices.RuntimeInformation");
        if (runtimeInformationType != null)
        {
            var frameworkDescriptionProperty = runtimeInformationType.GetProperty("FrameworkDescription", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if (frameworkDescriptionProperty != null)
            {
                FrameworkDescription = (string)frameworkDescriptionProperty.GetValue(null, null);

                if (FrameworkDescription.StartsWith(".NET ", StringComparison.Ordinal) && char.IsDigit(FrameworkDescription[5]))
                {
                    if (Type.GetType("Mono.RuntimeStructs") != null)
                    {
                        FrameworkDescription += " (Mono)";
                    }
                    else if (IsDynamicCodeCompiled() == false)
                    {
                        FrameworkDescription += " (NativeAOT)";
                    }
                    else
                    {
                        FrameworkDescription += " (CoreCLR)";
                    }
                }
            }
            else
            {
                FrameworkDescription = "<unknown>";
            }
        }
    }

    private static bool? IsDynamicCodeCompiled()
    {
        var runtimeFeatureType = Type.GetType("System.Runtime.CompilerServices.RuntimeFeature");
        if (runtimeFeatureType != null)
        {
            var isDynamicCodeCompiledProperty = runtimeFeatureType.GetProperty("IsDynamicCodeCompiled", BindingFlags.Public | BindingFlags.Static);
            if (isDynamicCodeCompiledProperty != null)
            {
                return (bool)isDynamicCodeCompiledProperty.GetValue(null, null);
            }
        }

        return null;
    }
}
