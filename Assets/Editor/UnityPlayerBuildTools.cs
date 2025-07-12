using System;
using System.Collections.Generic;
using System.Linq;
using Buildalon.Editor.BuildPipeline;
using Buildalon.Editor.BuildPipeline.Logging;
using JetBrains.Annotations;
using UnityEditor;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build;
#endif
using UnityEditor.Build.Reporting;
using UnityEngine;

internal class UnityPlayerBuildTools
#if UNITY_2018_1_OR_NEWER
    : IPreprocessBuildWithReport, IPostprocessBuildWithReport
#endif
{
    // Build configurations. Exactly one of these should be defined for any given build.
    public const string BuildSymbolDebug = "debug";
    public const string BuildSymbolRelease = "release";
    public const string BuildSymbolMaster = "master";

    private static IBuildInfo buildInfo;

    /// <summary>
    /// Gets or creates an instance of the <see cref="IBuildInfo"/> to use when building.
    /// </summary>
    /// <returns>A new instance of <see cref="IBuildInfo"/>.</returns>
    public static IBuildInfo BuildInfo
    {
        get
        {
            BuildInfo buildInfoInstance;
            var currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            var isBuildInfoNull = buildInfo == null;

            if (isBuildInfoNull ||
                buildInfo.BuildTarget != currentBuildTarget)
            {
                switch (currentBuildTarget)
                {
                    case BuildTarget.Android:
                        buildInfoInstance = new AndroidBuildInfo();
                        break;
                    case BuildTarget.iOS:
                        buildInfoInstance = new IOSBuildInfo();
                        break;
                    // case BuildTarget.WSAPlayer:
                    //     buildInfoInstance = new WSAPlayerBuildInfo();
                    //     break;
                    // TODO: Add additional platform specific build info classes as needed
                    default:
                        buildInfoInstance = new BuildInfo();
                        break;
                }
            }
            else
            {
                buildInfoInstance = buildInfo as BuildInfo;
            }

            if (buildInfoInstance == null)
            {
                return null;
            }

            buildInfo = buildInfoInstance;
            Debug.Assert(buildInfo != null);

            return buildInfo;
        }
        internal set { buildInfo = value; }
    }

    /// <summary>
    /// Start a build using command line arguments.
    /// </summary>
    [UsedImplicitly]
    public static void StartCommandLineBuild()
    {
        // We don't need stack traces on all our logs. Makes things a lot easier to read.
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Debug.Log("Starting command line build...");

        var buildReports = new HashSet<BuildReport>();

#if UNITY_2018_1_OR_NEWER
        void CommandLineBuildReportCallback(BuildReport postProcessBuildReport)
        {
            if (postProcessBuildReport != null)
            {
                buildReports.Add(postProcessBuildReport);
            }
        }

        OnBuildCompletedWithSummary += CommandLineBuildReportCallback;
#endif

        BuildReport finalBuildReport = null;
        var failed = false;

        try
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                EditorPrefs.SetString("AndroidSdkRoot", Environment.GetEnvironmentVariable("ANDROID_HOME"));
                EditorPrefs.SetString("AndroidNdkRoot", Environment.GetEnvironmentVariable("ANDROID_NDK_HOME"));
                EditorPrefs.SetString("JdkPath", Environment.GetEnvironmentVariable("JAVA_HOME"));

                var androidSdkPath = EditorPrefs.GetString("AndroidSdkRoot",
#if UNITY_EDITOR_WIN
                        @"C:\Program Files (x86)\Android\android-sdk"
#else
                    string.Empty
#endif
                );

                Debug.Log(string.Format("AndroidSdkRoot: {0}", androidSdkPath));
                Debug.Log(string.Format("AndroidNdkRoot: {0}", EditorPrefs.GetString("AndroidNdkRoot")));
            }

            finalBuildReport = BuildUnityPlayer();

#if !UNITY_2018_1_OR_NEWER
            buildReports.Add(finalBuildReport);
            if (!StringEx.IsNullOrWhiteSpace(finalBuildReport.errors))
            {
                Debug.LogError(finalBuildReport.errors);
            }
#endif
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format("Build Failed!\n{0}\n{1}", e.Message, e.StackTrace));
            failed = true;
        }
#if UNITY_2018_1_OR_NEWER
        finally
        {
            OnBuildCompletedWithSummary -= CommandLineBuildReportCallback;
        }
#endif

        if (buildReports.Count == 0)
        {
            Debug.LogError("Failed to find any valid build reports!");
            EditorApplication.Exit(1);
            return;
        }
        else
        {
#if UNITY_2018_1_OR_NEWER
            CommandLineBuildReportCallback(finalBuildReport);
#endif

            foreach (var buildReport in buildReports)
            {
                CILoggingUtility.GenerateBuildReport(buildReport, null);
            }
        }

        Debug.Log("Exiting command line build...");
        var success = buildReports.All(report => report.summary.result == BuildResult.Succeeded) && !failed;
        EditorApplication.Exit(success ? 0 : 1);
    }

    public static BuildReport BuildUnityPlayer()
    {
        if (BuildInfo == null)
        {
            throw new ArgumentNullException("BuildInfo");
        }

        EditorUtility.DisplayProgressBar(string.Format("{0} Build Pipeline", BuildInfo.BuildTarget), "Gathering Build Data...", 0.25f);

        if (BuildInfo.IsCommandLine)
        {
            BuildInfo.ParseCommandLineArgs();
        }

        // use https://semver.org/
        // major.minor.build
        var version = new Version(
            (buildInfo.Version == null || buildInfo.AutoIncrement)
                ? StringEx.IsNullOrWhiteSpace(PlayerSettings.bundleVersion)
                    ? GetValidVersionString(Application.version)
                    : GetValidVersionString(PlayerSettings.bundleVersion)
                : GetValidVersionString(buildInfo.Version.ToString()));

        // Only auto incitement if the version wasn't specified in the build info.
        if (buildInfo.Version == null &&
            buildInfo.AutoIncrement)
        {
            version = new Version(version.Major, version.Minor, version.Build + 1);
        }

        // Updates the Application.version and syncs Android and iOS bundle version strings
        PlayerSettings.bundleVersion = version.ToString();
        // // Update Lumin bc the Application.version isn't synced like Android & iOS
        // PlayerSettings.Lumin.versionName = PlayerSettings.bundleVersion;
        // Update WSA bc the Application.version isn't synced like Android & iOS
        PlayerSettings.WSA.packageVersion = new Version(version.Major, version.Minor, version.Build, 0);
#if UNITY_6000_0_OR_NEWER
            PlayerSettings.visionOSBundleVersion = PlayerSettings.bundleVersion;
#endif // UNITY_6000_0_OR_NEWER

        var buildTargetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(buildInfo.BuildTarget);
#if UNITY_2023_1_OR_NEWER
            var oldBuildIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));
#else
        var oldBuildIdentifier = PlayerSettings.GetApplicationIdentifier(buildTargetGroup);
#endif // UNITY_2023_1_OR_NEWER

        if (!StringEx.IsNullOrWhiteSpace(buildInfo.BundleIdentifier))
        {
#if UNITY_2023_1_OR_NEWER
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), buildInfo.BundleIdentifier);
#else
            PlayerSettings.SetApplicationIdentifier(buildTargetGroup, buildInfo.BundleIdentifier);
#endif // UNITY_2023_1_OR_NEWER
        }

#if UNITY_2023_1_OR_NEWER
            var playerBuildSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));
#else
        var playerBuildSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
#endif // UNITY_2023_1_OR_NEWER

        if (!string.IsNullOrEmpty(playerBuildSymbols))
        {
            if (buildInfo.HasConfigurationSymbol())
            {
                buildInfo.AppendWithoutConfigurationSymbols(playerBuildSymbols);
            }
            else
            {
                buildInfo.AppendSymbols(playerBuildSymbols.Split(';'));
            }
        }

        if (!string.IsNullOrEmpty(buildInfo.BuildSymbols))
        {
#if UNITY_2023_1_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), buildInfo.BuildSymbols);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, buildInfo.BuildSymbols);
#endif // UNITY_2023_1_OR_NEWER
        }

        if ((buildInfo.BuildOptions & BuildOptions.Development) == BuildOptions.Development &&
            !buildInfo.HasConfigurationSymbol())
        {
            buildInfo.AppendSymbols(BuildSymbolDebug);
        }

        if (buildInfo.HasAnySymbols(BuildSymbolDebug))
        {
            buildInfo.BuildOptions |= BuildOptions.Development | BuildOptions.AllowDebugging;
        }

        if (buildInfo.HasAnySymbols(BuildSymbolRelease))
        {
            // Unity automatically adds the DEBUG symbol if the BuildOptions.Development flag is
            // specified. In order to have debug symbols and the RELEASE symbols we have to
            // inject the symbol Unity relies on to enable the /debug+ flag of csc.exe which is "DEVELOPMENT_BUILD"
            buildInfo.AppendSymbols("DEVELOPMENT_BUILD");
        }

        var oldColorSpace = PlayerSettings.colorSpace;

        if (buildInfo.ColorSpace.HasValue)
        {
            PlayerSettings.colorSpace = buildInfo.ColorSpace.Value;
        }

        BuildReport buildReport = null;

        if (ApplicationEx.isBatchMode)
        {
            Debug.Log(string.Format("Build Target: {0}", buildInfo.BuildTarget));
            Debug.Log(string.Format("Build Options: {0}", buildInfo.BuildOptions));
            Debug.Log(string.Format("Target output: \"{0}\"", buildInfo.FullOutputPath));
            Debug.Log(string.Format("Scenes in build:\n{0}", string.Join("\n    ", buildInfo.Scenes.Select(scene => scene.path).ToArray())));
        }

        try
        {
#if UNITY_ADDRESSABLES
                UnityEditor.AddressableAssets.Build.BuildScript.buildCompleted += OnAddressableBuildResult;
#endif
            buildReport = BuildPipelineEx.BuildPlayer(
                buildInfo.Scenes.ToArray(),
                buildInfo.FullOutputPath,
                buildInfo.BuildTarget,
                buildInfo.BuildOptions);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
#if UNITY_ADDRESSABLES
                UnityEditor.AddressableAssets.Build.BuildScript.buildCompleted -= OnAddressableBuildResult;
#endif
            PlayerSettings.colorSpace = oldColorSpace;

#if UNITY_2023_1_OR_NEWER
                if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup)) != oldBuildIdentifier)
                {
                    PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), oldBuildIdentifier);
                }
#else
            if (PlayerSettings.GetApplicationIdentifier(buildTargetGroup) != oldBuildIdentifier)
            {
                PlayerSettings.SetApplicationIdentifier(buildTargetGroup, oldBuildIdentifier);
            }
#endif // UNITY_2023_1_OR_NEWER
        }

        return buildReport;
    }

    public static string GetValidVersionString(string version)
    {
        if (StringEx.IsNullOrWhiteSpace(version))
        {
            return "1.0.0";
        }

        var parts = version.Split('.');

        switch (parts.Length)
        {
            case 0:
                return "1.0.0";
            case 1:
                return string.Format("{0}.0.0", parts[0]);
            case 2:
                return string.Format("{0}.{1}.0", parts[0], parts[1]);
            case 3:
                return string.Format("{0}.{1}.{2}", parts[0], parts[1], parts[2].Replace("-preview", string.Empty));
            default:
                return string.Format("{0}.{1}.{2}.{3}", parts[0], parts[1], parts[2].Replace("-preview", string.Empty), parts[parts.Length - 1]);
        }
    }

    /// <summary>
    /// Splits the scene list provided in CSV format to an array of scene path strings.
    /// </summary>
    /// <param name="sceneList">A CSV list of scenes to split.</param>
    /// <returns>An array of scene path strings.</returns>
    public static IEnumerable<EditorBuildSettingsScene> SplitSceneList(string sceneList)
    {
        var sceneListArray = sceneList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        return sceneListArray
            .Where(scenePath => !StringEx.IsNullOrWhiteSpace(scenePath))
            .Select(scene => new EditorBuildSettingsScene(scene.Trim(), true));
    }

#if UNITY_2018_1_OR_NEWER
    #region IOrderedCallback

    /// <inheritdoc />
    public int callbackOrder { get; }

    /// <inheritdoc />
    public void OnPreprocessBuild(BuildReport report)
    {
        if (buildInfo == null)
        {
            return;
        }

        // set build number
        if (!string.IsNullOrWhiteSpace(buildInfo.BuildNumber))
        {
#if PLATFORM_ANDROID
                if (int.TryParse(buildInfo.BuildNumber, out var code))
                {
                    PlayerSettings.Android.bundleVersionCode = code;
                }
                else
                {
                    Debug.LogError(string.Format("Failed to parse versionCode \"{0}\"", buildInfo.BuildNumber));
                }
            }
            else if (buildInfo.AutoIncrement)
            {
                PlayerSettings.Android.bundleVersionCode++;
#else // ANY OTHER PLATFORM
            PlayerSettings.iOS.buildNumber = buildInfo.BuildNumber;
            PlayerSettings.macOS.buildNumber = buildInfo.BuildNumber;
            PlayerSettings.tvOS.buildNumber = buildInfo.BuildNumber;
#if UNITY_6000_0_OR_NEWER
                PlayerSettings.VisionOS.buildNumber = buildInfo.BuildNumber;
#endif // UNITY_6000_0_OR_NEWER
#endif // ANY OTHER PLATFORM
        }

        buildInfo.OnPreProcessBuild(report);
    }

    /// <inheritdoc />
    public void OnPostprocessBuild(BuildReport report)
    {
        buildInfo?.OnPostProcessBuild(report);
        OnBuildCompletedWithSummary?.Invoke(report);
    }

    public static event Action<BuildReport> OnBuildCompletedWithSummary;

    #endregion IOrderedCallback
#endif
}
