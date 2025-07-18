#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
#define UNITY_5_0_OR_NEWER
#endif

using System;
using System.Linq;
using UnityEditor;
#if UNITY_5_3_OR_NEWER
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using Debug = UnityEngine.Debug;

internal class TestGame : AssetPostprocessor
{
    public static void SetupAndBuild()
    {
#if UNITY_5_4_OR_NEWER
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
#endif
        Debug.Log("TestGame.SetupAndBuild");

        EditorSettings.serializationMode = SerializationMode.ForceText;

        PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.HiddenByDefault;
        PlayerSettings.defaultIsFullScreen = false;
        PlayerSettings.resizableWindow = true;

#if UNITY_5_5_OR_NEWER
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
#endif

        PlayerSettings.companyName = "BepInEx";
        PlayerSettings.productName = "TestGame";
#if UNITY_5_6_OR_NEWER
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "TestGame");
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "dev.bepinex.testgame");
#else
        PlayerSettings.bundleIdentifier = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android
            ? "dev.bepinex.testgame"
            : "TestGame";
#endif

        var sha = Environment.GetEnvironmentVariable("GITHUB_SHA");
        PlayerSettings.bundleVersion = !StringEx.IsNullOrWhiteSpace(sha)
            ? sha.Substring(0, 12)
            : "local";

        CreateMainScene();
        BakeLighting();

#if UNITY_5_0_OR_NEWER
        UnityPlayerBuildTools.StartCommandLineBuild();
#else
        // "Building Player from editor scripts requires Unity PRO" 
#endif
    }

    private static void CreateMainScene()
    {
#if UNITY_5_3_OR_NEWER
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
#else
        EditorApplication.NewScene();

        var light = new GameObject("Directional Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.5f;
#endif

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Cube";
        cube.AddComponent<RotatingCubeBehaviour>();
        cube.AddComponent<DebugInfoBehaviour>().font = (Font)AssetDatabase.LoadAssetAtPath("Assets/Liberation/LiberationSans-Regular.ttf", typeof(Font));

        var path = "Assets/MainScene.unity";

#if UNITY_5_3_OR_NEWER
        if (!EditorSceneManager.SaveScene(scene, path))
        {
            throw new Exception("Failed to save the scene!");
        }
#else
        EditorApplication.SaveScene(path);
#endif

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(path, true),
        };

#if UNITY_5_3_OR_NEWER
        EditorSceneManager.MarkAllScenesDirty();
#endif
    }

    private static void BakeLighting()
    {
        Lightmapping.Clear();
#if UNITY_5_0_OR_NEWER
        Lightmapping.ClearDiskCache();
#endif
#if UNITY_5_3_OR_NEWER
        Lightmapping.ClearLightingDataAsset();
#endif

#if UNITY_5_0_OR_NEWER
        Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
#endif

        if (Environment.GetCommandLineArgs().Contains("-nographics", StringComparer.OrdinalIgnoreCase))
        {
            Debug.LogWarning("Running with -nographics, lighting data will be incorrect");
        }

#if UNITY_EDITOR_OSX && UNITY_2021_1_OR_NEWER && !UNITY_2023_1_OR_NEWER
        Debug.LogWarning("Unity 2021/2022's light baking hangs on MacOS, for some reason, skipping");
        return;
#endif

        Debug.Log("Calling Lightmapping.Bake");
        if (!Lightmapping.Bake())
        {
            Debug.LogWarning("Lightmapping.Bake failed");
        }
    }

    // Disables sln/csproj generation
    public static bool OnPreGeneratingCSProjectFiles()
    {
        return true;
    }
}
