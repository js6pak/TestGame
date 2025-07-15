using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class TestGame
{
    public static void SetupAndBuild()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Debug.Log("TestGame.SetupAndBuild");

        EditorSettings.serializationMode = SerializationMode.ForceText;

        PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.HiddenByDefault;
        PlayerSettings.defaultIsFullScreen = false;
        PlayerSettings.resizableWindow = true;

        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;

        PlayerSettings.companyName = "BepInEx";
        PlayerSettings.productName = "TestGame";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "TestGame");
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "dev.bepinex.testgame");

        var sha = Environment.GetEnvironmentVariable("GITHUB_SHA");
        PlayerSettings.bundleVersion = !StringEx.IsNullOrWhiteSpace(sha)
            ? sha.Substring(0, 12)
            : "local";

        CreateMainScene();
        BakeLighting();

        UnityPlayerBuildTools.StartCommandLineBuild();
    }

    private static void CreateMainScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Cube";
        cube.AddComponent<RotatingCubeBehaviour>();
        cube.AddComponent<DebugInfoBehaviour>().font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Liberation/LiberationSans-Regular.ttf");

        if (!EditorSceneManager.SaveScene(scene, "Assets/MainScene.unity"))
        {
            throw new Exception("Failed to save the scene!");
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(scene.path, true),
        };

        EditorSceneManager.MarkAllScenesDirty();
    }

    private static void BakeLighting()
    {
        Lightmapping.Clear();
        Lightmapping.ClearDiskCache();
        Lightmapping.ClearLightingDataAsset();

        Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;

        if (Environment.GetCommandLineArgs().Contains("-nographics", StringComparer.OrdinalIgnoreCase))
        {
            Debug.LogWarning("Running with -nographics, lighting data will be incorrect");
        }

        Debug.Log("Calling Lightmapping.Bake");
        if (!Lightmapping.Bake())
        {
            Debug.LogWarning("Lightmapping.Bake failed");
        }
    }
}
