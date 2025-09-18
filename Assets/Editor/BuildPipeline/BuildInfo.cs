// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
#define UNITY_5_0_OR_NEWER
#endif

using Buildalon.Editor.BuildPipeline.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Buildalon.Editor.BuildPipeline
{
    /// <summary>
    /// A generic build info class.
    /// </summary>
    public class BuildInfo : IBuildInfo
    {
        /// <inheritdoc />
        public bool AutoIncrement { get; set; }

        private string bundleIdentifier;

        /// <inheritdoc />
        public string BundleIdentifier
        {
            get
            {
                if (StringEx.IsNullOrWhiteSpace(bundleIdentifier))
                {
#if UNITY_5_6_OR_NEWER
                    bundleIdentifier = PlayerSettings.applicationIdentifier;
#else
                    bundleIdentifier = PlayerSettings.bundleIdentifier;
#endif
                }

                return bundleIdentifier;
            }
            set
            {
                bundleIdentifier = value;
#if UNITY_5_6_OR_NEWER
                PlayerSettings.applicationIdentifier = bundleIdentifier;
#else
                PlayerSettings.bundleIdentifier = bundleIdentifier;
#endif
            }
        }

        /// <inheritdoc />
        public virtual Version Version { get; set; }

        /// <inheritdoc />
        public virtual string BuildNumber { get; set; }

        /// <inheritdoc />
        public virtual BuildTarget BuildTarget { get; private set; }

        /// <inheritdoc />
        public virtual BuildTargetGroup BuildTargetGroup { get; private set; }

        /// <inheritdoc />
        public bool IsCommandLine { get; private set; }

        private string outputDirectory;

        /// <inheritdoc />
        public virtual string OutputDirectory
        {
            get
            {
                return string.IsNullOrEmpty(outputDirectory)
                    ? outputDirectory = string.Format("Builds/{0}", BuildTarget)
                    : outputDirectory;
            }
            set
            {
                var projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/");
                var newValue = value != null ? value.Replace("\\", "/") : null;

                outputDirectory = !StringEx.IsNullOrWhiteSpace(newValue) && Path.IsPathRooted(newValue)
                    ? newValue.Contains(projectRoot)
                        ? newValue.Replace(string.Format("{0}/", projectRoot), string.Empty)
                        : GetRelativePath(projectRoot, newValue)
                    : newValue;
            }
        }

        private static string GetRelativePath(string fromPath, string toPath)
        {
#if UNITY_2021_2_OR_NEWER
            return Path.GetRelativePath(fromPath, toPath).Replace("\\", "/");
#else
            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);
            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
            return relativePath.Replace("\\", "/");
#endif // UNITY_2021_1_OR_NEWER
        }

        /// <inheritdoc />
        public virtual string AbsoluteOutputDirectory
        {
            get
            {
                var rootBuildDirectory = OutputDirectory;
                var dirCharIndex = rootBuildDirectory.IndexOf(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);

                if (dirCharIndex != -1)
                {
                    rootBuildDirectory = rootBuildDirectory.Substring(0, dirCharIndex);
                }

                return Path.GetFullPath(Path.Combine(Path.Combine(Application.dataPath, ".."), rootBuildDirectory));
            }
        }

        /// <inheritdoc />
        public virtual string FullOutputPath
        {
            get { return string.Format("{0}{1}{2}{3}", OutputDirectory, Path.DirectorySeparatorChar, BundleIdentifier, ExecutableFileExtension); }
        }

        /// <inheritdoc />
        public virtual string ExecutableFileExtension
        {
            get
            {
                switch (BuildTarget)
                {
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
// #if PLATFORM_STANDALONE_WIN
                        // return UnityEditor.WindowsStandalone.UserBuildSettings.createSolution ? string.Format("{0}{1}", Path.DirectorySeparatorChar, Application.productName) : ".exe";
// #else
                        return ".exe";
// #endif
#if UNITY_2017_3_OR_NEWER
                    case BuildTarget.StandaloneOSX:
#elif !UNITY_4_0 // UNITY_4_1_OR_NEWER
                    case BuildTarget.StandaloneOSXUniversal:
#else
                    case BuildTarget.StandaloneOSXIntel:
#endif
// #if PLATFORM_STANDALONE_OSX
                        // return UnityEditor.OSXStandalone.UserBuildSettings.createXcodeProject ? string.Format("{0}{1}", Path.DirectorySeparatorChar, Application.productName) : ".app";
// #else
                        return ".app";
// #endif
#if !UNITY_2019_2_OR_NEWER
                    case BuildTarget.StandaloneLinux:
#endif
                    case BuildTarget.StandaloneLinux64:
                        return string.Empty;
                    default:
                        return Path.DirectorySeparatorChar.ToString();
                }
            }
        }

        private List<EditorBuildSettingsScene> scenes;

        /// <inheritdoc />
        public IEnumerable<EditorBuildSettingsScene> Scenes
        {
            get
            {
                if (scenes == null || !scenes.Any())
                {
                    scenes = EditorBuildSettings.scenes.Where(scene => !StringEx.IsNullOrWhiteSpace(scene.path)).Where(scene => scene.enabled).ToList();
                }

                return scenes;
            }
            set { scenes = value.ToList(); }
        }

        /// <inheritdoc />
        public BuildOptions BuildOptions { get; set; }

        /// <inheritdoc />
        public ColorSpace? ColorSpace { get; set; }

        /// <inheritdoc />
        public string BuildSymbols { get; set; }

        public BuildInfo()
        {
            BuildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            IsCommandLine = ApplicationEx.isBatchMode;
            BuildSymbols = string.Empty;
        }

        /// <inheritdoc />
        public virtual void ParseCommandLineArgs()
        {
            var arguments = Environment.GetCommandLineArgs();

            for (int i = 0; i < arguments.Length; ++i)
            {
                switch (arguments[i])
                {
                    case "-ignorecompilererrors":
                        CILoggingUtility.LoggingEnabled = false;
                        break;
                    case "-autoIncrement":
                        AutoIncrement = true;
                        break;
                    case "-versionName":
                        var versionString = UnityPlayerBuildTools.GetValidVersionString(arguments[++i]);

                        Version version;
                        if (VersionEx.TryParse(versionString, out version))
                        {
                            Version = version;
                        }
                        else
                        {
                            Debug.LogError(string.Format("Failed to parse -versionName \"{0}\"", arguments[i]));
                        }

                        break;
                    case "-buildNumber":
                        BuildNumber = arguments[++i];
                        break;
                    case "-bundleIdentifier":
                        BundleIdentifier = arguments[++i];
                        break;
                    case "-sceneList":
                        Scenes = UnityPlayerBuildTools.SplitSceneList(arguments[++i]);
                        break;
                    case "-sceneListFile":
                        Scenes = UnityPlayerBuildTools.SplitSceneList(File.ReadAllText(arguments[++i]));
                        break;
                    case "-buildOutputDirectory":
                        OutputDirectory = arguments[++i] != null ? arguments[++i].Replace("'", string.Empty).Replace("\"", string.Empty) : null;
                        break;
                    case "-acceptExternalModificationsToPlayer":
                        BuildOptions = BuildOptions.SetFlag(BuildOptions.AcceptExternalModificationsToPlayer);
                        break;
                    case "-development":
                        EditorUserBuildSettings.development = true;
                        BuildOptions = BuildOptions.SetFlag(BuildOptions.Development);
                        break;
                    case "-colorSpace":
                        ColorSpace = (ColorSpace)Enum.Parse(typeof(ColorSpace), arguments[++i]);
                        break;
                    case "-compressionMethod":
                        var compressionMethod = arguments[++i].ToLower();
                        switch (compressionMethod.ToUpper())
                        {
#if UNITY_2017_2_OR_NEWER
                            case "LZ4HC":
                                BuildOptions = BuildOptions.SetFlag(BuildOptions.CompressWithLz4HC);
                                break;
#endif
#if UNITY_5_6_OR_NEWER
                            case "LZ4":
                                BuildOptions = BuildOptions.SetFlag(BuildOptions.CompressWithLz4);
                                break;
#endif
                            default:
                                Debug.LogError(string.Format("Failed to parse -compressionMethod: \"{0}\"", compressionMethod));
                                break;
                        }

                        break;
                    case "-buildConfiguration":
                        var configuration = arguments[++i].Substring(1).ToLower();

                        switch (configuration)
                        {
                            case "debug":
                            case "master":
                            case "release":
                                Configuration = configuration;
                                break;
                            default:
                                Debug.LogError(string.Format("Failed to parse -buildConfiguration: \"{0}\"", configuration));
                                break;
                        }

                        break;
                    case "-export":
// TODO this was added somewhere in between 2018.4 and 2019.4, figure out when and ifdef
// #if PLATFORM_STANDALONE_WIN
//                         UnityEditor.WindowsStandalone.UserBuildSettings.createSolution = true;
// #elif PLATFORM_STANDALONE_OSX
//                         UnityEditor.OSXStandalone.UserBuildSettings.createXcodeProject = true;
// #endif
                        break;
                    case "-symlinkSources":
#if UNITY_2021_2_OR_NEWER
                        EditorUserBuildSettings.symlinkSources = true;
#else
                        EditorUserBuildSettings.symlinkLibraries = true;
#endif // UNITY_2021_1_OR_NEWER
                        break;
                    case "-disableDebugging":
                        EditorUserBuildSettings.allowDebugging = false;
                        Debug.LogWarning("This arg has been deprecated. use \"-allowDebugging false\" instead.");
                        break;
                    case "-allowDebugging":
                        var value = arguments[++i];

                        switch (value.ToLower())
                        {
                            case "true":
                                EditorUserBuildSettings.allowDebugging = true;
                                break;
                            case "false":
                                EditorUserBuildSettings.allowDebugging = false;
                                break;
                            default:
                                Debug.LogError(string.Format("Failed to parse -allowDebugging: \"{0}\"", value));
                                break;
                        }

                        break;
#if UNITY_2018_1_OR_NEWER
                    case "-il2cppCompilerConfiguration":
                        var il2CppCompilerConfigurationString = arguments[++i];

                        Il2CppCompilerConfiguration config;
                        if (EnumEx.TryParse(il2CppCompilerConfigurationString, true, out config))
                        {
#if UNITY_6000_0_OR_NEWER
                            PlayerSettings.SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup), config);
#else
                            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup, config);
#endif // UNITY_6000_0_OR_NEWER
                        }
                        else
                        {
                            Debug.LogError(string.Format("Failed to parse -il2cppCompilerConfiguration: \"{0}\"", il2CppCompilerConfigurationString));
                        }

                        break;
#endif
#if UNITY_2022_1_OR_NEWER
                    case "-il2cppCodeGeneration":
                        var il2CppCodeGenerationString = arguments[++i];

                        if (Enum.TryParse(il2CppCodeGenerationString, true, out UnityEditor.Build.Il2CppCodeGeneration apiCompatibilityLevel))
                        {
                            PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup), apiCompatibilityLevel);
                        }
                        else
                        {
                            Debug.LogError(string.Format("Failed to parse -il2cppCodeGeneration: \"{0}\"", il2CppCodeGenerationString));
                        }

                        break;
#endif // UNITY_2022_1_OR_NEWER
                    case "-dotnetApiCompatibilityLevel":
                        var apiCompatibilityLevelString = arguments[++i];

                        ApiCompatibilityLevel apiCompatibility;
                        if (EnumEx.TryParse(apiCompatibilityLevelString, true, out apiCompatibility))
                        {
#if UNITY_2023_1_OR_NEWER
                            PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup), apiCompatibility);
#elif UNITY_5_6_OR_NEWER
                            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup, apiCompatibility);
#else
                            PlayerSettings.apiCompatibilityLevel = apiCompatibility;
#endif // UNITY_2023_1_OR_NEWER
                        }
                        else
                        {
                            Debug.LogError(string.Format("Failed to parse -dotnetApiCompatibilityLevel: \"{0}\"", apiCompatibilityLevelString));
                        }

                        break;
                    case "-scriptingBackend":
#if UNITY_5_0_OR_NEWER
                        var scriptingBackendString = arguments[++i].ToLower();

                        switch (scriptingBackendString)
                        {
                            case "mono":
                            case "mono2x":
#if UNITY_2023_1_OR_NEWER
                                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup), ScriptingImplementation.Mono2x);
#elif UNITY_5_6_OR_NEWER
                                PlayerSettings.SetScriptingBackend(BuildTargetGroup, ScriptingImplementation.Mono2x);
#else
                                PlayerSettings.SetPropertyInt("ScriptingBackend", (int)ScriptingImplementation.Mono2x, BuildTargetGroup);
#endif // UNITY_2023_1_OR_NEWER
                                break;
                            case "il2cpp":
#if UNITY_2023_1_OR_NEWER
                                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup), ScriptingImplementation.IL2CPP);
#elif UNITY_5_6_OR_NEWER
                                PlayerSettings.SetScriptingBackend(BuildTargetGroup, ScriptingImplementation.IL2CPP);
#else
                                PlayerSettings.SetPropertyInt("ScriptingBackend", (int)ScriptingImplementation.IL2CPP, BuildTargetGroup);
#endif // UNITY_2023_1_OR_NEWER
                                break;
                            case "winrt":
#if UNITY_2023_1_OR_NEWER
                                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup), ScriptingImplementation.WinRTDotNET);
#elif UNITY_5_6_OR_NEWER
                                PlayerSettings.SetScriptingBackend(BuildTargetGroup, ScriptingImplementation.WinRTDotNET);
#else
                                PlayerSettings.SetPropertyInt("ScriptingBackend", (int)ScriptingImplementation.WinRTDotNET, BuildTargetGroup);
#endif // UNITY_2023_1_OR_NEWER
                                break;
                            default:
                                Debug.LogError(string.Format("Unsupported -scriptingBackend: \"{0}\"", scriptingBackendString));
                                break;
                        }
#endif

                        break;
                    case "-autoConnectProfiler":
                        EditorUserBuildSettings.connectProfiler = true;
                        BuildOptions = BuildOptions.SetFlag(BuildOptions.ConnectWithProfiler);
                        break;
#if UNITY_2019_3_OR_NEWER
                    case "-buildWithDeepProfilingSupport":
                        EditorUserBuildSettings.buildWithDeepProfilingSupport = true;
                        BuildOptions = BuildOptions.SetFlag(BuildOptions.EnableDeepProfilingSupport);
                        break;
#endif
#if UNITY_5_5_OR_NEWER
                    case "-appleTeamId":
                        var teamId = arguments[++i];
                        PlayerSettings.iOS.appleDeveloperTeamID = teamId;
                        break;
                    case "-enableAppleAutomaticSigning":
                        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
                        break;
                    case "-disableAppleAutomaticSigning":
                        PlayerSettings.iOS.appleEnableAutomaticSigning = false;
                        break;
                    case "-appleProvisioningProfileId":
                        var profileId = arguments[++i];

                        if (BuildTarget == BuildTarget.tvOS)
                        {
                            PlayerSettings.iOS.tvOSManualProvisioningProfileID = profileId;
                        }
                        else
                        {
                            PlayerSettings.iOS.iOSManualProvisioningProfileID = profileId;
                        }

                        break;
#endif
#if UNITY_2018_1_OR_NEWER
                    case "-appleProvisioningProfileType":
                        var profileType = arguments[++i].ToLower();

                        if (BuildTarget == BuildTarget.tvOS)
                        {
                            switch (profileType)
                            {
                                case "automatic":
                                    PlayerSettings.iOS.tvOSManualProvisioningProfileType = ProvisioningProfileType.Automatic;
                                    break;
                                case "development":
                                    PlayerSettings.iOS.tvOSManualProvisioningProfileType = ProvisioningProfileType.Development;
                                    break;
                                case "distribution":
                                    PlayerSettings.iOS.tvOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
                                    break;
                                default:
                                    Debug.LogError(string.Format("Unsupported -appleProvisioningProfileType: \"{0}\"", profileType));
                                    break;
                            }
                        }
                        else
                        {
                            switch (profileType)
                            {
                                case "automatic":
                                    PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Automatic;
                                    break;
                                case "development":
                                    PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Development;
                                    break;
                                case "distribution":
                                    PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
                                    break;
                                default:
                                    Debug.LogError(string.Format("Unsupported -appleProvisioningProfileType: \"{0}\"", profileType));
                                    break;
                            }
                        }

                        break;
#endif
                    case "-appleSdkVersion":
                        var sdk = arguments[++i].ToLower();

                        switch (sdk)
                        {
                            case "device":
                                PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
                                break;
                            case "simulator":
                                PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
                                break;
                            default:
                                Debug.LogError(string.Format("Unsupported -appleSdk: \"{0}\"", sdk));
                                break;
                        }

                        break;
                    case "-arch":
                        var arch = arguments[++i].ToLower();

#if PLATFORM_STANDALONE_OSX
#if UNITY_2020_2_OR_NEWER
                        switch (arch)
                        {
#if UNITY_2022_2_OR_NEWER
                            case "x64": UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64; break;
                            case "arm64": UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.ARM64; break;
                            case "x64arm64": UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64ARM64; break;
#else
                            case "x64": UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.OSXStandalone.MacOSArchitecture.x64; break;
                            case "arm64": UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.OSXStandalone.MacOSArchitecture.ARM64; break;
                            case "x64arm64": UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.OSXStandalone.MacOSArchitecture.x64ARM64; break;
#endif // UNITY_2022_2_OR_NEWER
                            default: throw new Exception(string.Format("Unsupported architecture: {0}", arch));
                        }
#elif UNITY_2017_1_OR_NEWER
                        switch (arch)
                        {
                            case "x64":
                                PlayerSettings.SetArchitecture(BuildTargetGroup, 0);
                                break;
                            case "arm64":
                                PlayerSettings.SetArchitecture(BuildTargetGroup, 1);
                                break;
                            case "x64arm64":
                                PlayerSettings.SetArchitecture(BuildTargetGroup, 2);
                                break;
                        }
#endif

#endif

#if PLATFORM_STANDALONE_WIN && UNITY_2023_1_OR_NEWER
                        UnityEditor.WindowsStandalone.UserBuildSettings.architecture = Enum.Parse<UnityEditor.Build.OSArchitecture>(arch, true);
#endif

                        break;
                }
            }
        }

        /// <inheritdoc />
        public virtual bool Install { get; set; }

        /// <inheritdoc />
        public string Configuration
        {
            get
            {
                if (!this.HasConfigurationSymbol())
                {
                    return UnityPlayerBuildTools.BuildSymbolMaster;
                }

                return this.HasAnySymbols(UnityPlayerBuildTools.BuildSymbolDebug)
                    ? UnityPlayerBuildTools.BuildSymbolDebug
                    : this.HasAnySymbols(UnityPlayerBuildTools.BuildSymbolRelease)
                        ? UnityPlayerBuildTools.BuildSymbolRelease
                        : UnityPlayerBuildTools.BuildSymbolMaster;
            }
            set
            {
                if (this.HasConfigurationSymbol())
                {
                    this.RemoveSymbols(new[]
                    {
                        UnityPlayerBuildTools.BuildSymbolDebug,
                        UnityPlayerBuildTools.BuildSymbolRelease,
                        UnityPlayerBuildTools.BuildSymbolMaster
                    });
                }

                this.AppendSymbols(value);
            }
        }

        /// <inheritdoc />
        public virtual void OnPreProcessBuild(BuildReport report)
        {
#if UNITY_6000_0_OR_NEWER
            var defaultIcons = PlayerSettings.GetIcons(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Unknown), IconKind.Any);
#elif UNITY_2017_1_OR_NEWER
            var defaultIcons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown, IconKind.Any);
#else
            var defaultIcons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);
#endif // UNITY_6000_0_OR_NEWER
            var icon = defaultIcons.Length > 0 ? defaultIcons[0] : null;
            if (icon != null)
            {
#if UNITY_2018_1_OR_NEWER
#if UNITY_6000_0_OR_NEWER
                var platformIconKinds = PlayerSettings.GetSupportedIconKinds(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup));
#else
                var platformIconKinds = PlayerSettings.GetSupportedIconKindsForPlatform(BuildTargetGroup);
#endif // UNITY_6000_0_OR_NEWER

                foreach (var platformIconKind in platformIconKinds)
                {
#if UNITY_6000_0_OR_NEWER
                    var platformIcons = PlayerSettings.GetPlatformIcons(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup), platformIconKind);
#else
                    var platformIcons = PlayerSettings.GetPlatformIcons(BuildTargetGroup, platformIconKind);
#endif // UNITY_6000_0_OR_NEWER

                    foreach (var platformIcon in platformIcons)
                    {
                        for (var i = 0; i < platformIcon.maxLayerCount; i++)
                        {
                            var texture = platformIcon.GetTexture(i);

                            if (texture != null)
                            {
                                continue;
                            }
#if PLATFORM_VISIONOS
                            var isBack = i == platformIcon.maxLayerCount - 1;
#else
                            var isBack = i == 0;
#endif
                            if (isBack && platformIcon.maxLayerCount > 1)
                            {
                                try
                                {
                                    Debug.LogWarning(string.Format("Setting {0}:{1} to Default-Checker-Gray", platformIcon.kind, platformIcon));
                                    var background = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Checker-Gray.png");
                                    platformIcon.SetTexture(background, i);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError(e);
                                }
                            }
                            else
                            {
                                Debug.LogWarning(string.Format("Setting {0}:{1} to default icon texture", platformIcon.kind, platformIcon));
                                platformIcon.SetTexture(icon, i);
                            }
                        }
                    }

#if UNITY_6000_0_OR_NEWER
                    PlayerSettings.SetPlatformIcons(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup), platformIconKind, platformIcons);
#else
                    PlayerSettings.SetPlatformIcons(BuildTargetGroup, platformIconKind, platformIcons);
#endif // UNITY_6000_0_OR_NEWER
                }
#endif
            }

            AssetDatabase.SaveAssets();
        }

        /// <inheritdoc />
        public virtual void OnPostProcessBuild(BuildReport report)
        {
        }
    }
}
