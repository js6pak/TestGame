// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Buildalon.Editor.BuildPipeline
{
    public class AndroidBuildInfo : BuildInfo
    {
        /// <inheritdoc />
        public override BuildTarget BuildTarget
        {
            get { return BuildTarget.Android; }
        }

        /// <inheritdoc />
        public override BuildTargetGroup BuildTargetGroup
        {
            get { return BuildTargetGroup.Android; }
        }

        /// <inheritdoc />
        public override string ExecutableFileExtension
        {
            get
            {
                return
#if UNITY_2017_4_OR_NEWER
                    EditorUserBuildSettings.buildAppBundle ? ".aab" :
#endif
                    ".apk";
            }
        }

        /// <inheritdoc />
        public override string FullOutputPath
        {
            get
            {
                return EditorUserBuildSettings.exportAsGoogleAndroidProject
#if UNITY_2018_2_OR_NEWER
                       || PlayerSettings.Android.buildApkPerCpuArchitecture
#endif
                    ? OutputDirectory
                    : base.FullOutputPath;
            }
        }

        public override void ParseCommandLineArgs()
        {
            base.ParseCommandLineArgs();

            var arguments = Environment.GetCommandLineArgs();
            var useCustomKeystore = false;

            for (int i = 0; i < arguments.Length; ++i)
            {
                switch (arguments[i])
                {
#if UNITY_2017_4_OR_NEWER
                    case "-appBundle":
                        EditorUserBuildSettings.buildAppBundle = true;
                        break;
                    case "-apkBundle":
                        EditorUserBuildSettings.buildAppBundle = false;
                        break;
#endif
#if UNITY_2018_2_OR_NEWER
                    case "-splitBinary":
                        Debug.LogWarning("-splitBinary is deprecated. Use -splitBinaryPerCpuArch instead.");
                        PlayerSettings.Android.buildApkPerCpuArchitecture = true;
                        break;
                    case "-splitBinaryPerCpuArch":
                        PlayerSettings.Android.buildApkPerCpuArchitecture = true;
                        break;
#endif
#if UNITY_6000_0_OR_NEWER
                    case "-minifyRelease":
                        PlayerSettings.Android.minifyRelease = true;
                        break;
                    case "-minifyDebug":
                        PlayerSettings.Android.minifyDebug = true;
                        break;
#endif
                    case "-splitApk":
                        Debug.LogWarning("-splitApk is deprecated. Use -splitApplicationBinary instead.");
#if UNITY_2023_1_OR_NEWER
                        PlayerSettings.Android.splitApplicationBinary = true;
#else
                        PlayerSettings.Android.useAPKExpansionFiles = true;
#endif // UNITY_2023_1_OR_NEWER
                        break;
                    case "-splitApplicationBinary":
#if UNITY_2023_1_OR_NEWER
                        PlayerSettings.Android.splitApplicationBinary = true;
#else
                        PlayerSettings.Android.useAPKExpansionFiles = true;
#endif // UNITY_2023_1_OR_NEWER
                        break;
                    case "-keystorePath":
                        PlayerSettings.Android.keystoreName = arguments[++i];
                        useCustomKeystore = true;
                        break;
                    case "-keystorePass":
                        PlayerSettings.Android.keystorePass = arguments[++i];
                        useCustomKeystore = true;
                        break;
                    case "-keyaliasName":
                        PlayerSettings.Android.keyaliasName = arguments[++i];
                        useCustomKeystore = true;
                        break;
                    case "-keyaliasPass":
                        PlayerSettings.Android.keyaliasPass = arguments[++i];
                        useCustomKeystore = true;
                        break;
                    case "-export":
                        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
                        break;
                    case "-symbols":
#if UNITY_6000_0_OR_NEWER
#if PLATFORM_ANDROID
                        var symbols = arguments[++i] switch
                        {
                            "public" => Unity.Android.Types.DebugSymbolLevel.SymbolTable,
                            "debugging" => Unity.Android.Types.DebugSymbolLevel.Full,
                            _ => Unity.Android.Types.DebugSymbolLevel.None
                        };

                        UnityEditor.Android.UserBuildSettings.DebugSymbols.level = symbols;
                        UnityEditor.Android.UserBuildSettings.DebugSymbols.format = Unity.Android.Types.DebugSymbolFormat.Zip;
#endif // PLATFORM_ANDROID
#elif UNITY_2018_4_OR_NEWER
#if UNITY_2021_2_OR_NEWER
                        var symbols = arguments[++i] switch
                        {
                            "public" => AndroidCreateSymbols.Public,
                            "debugging" => AndroidCreateSymbols.Debugging,
                            _ => AndroidCreateSymbols.Disabled
                        };
                        EditorUserBuildSettings.androidCreateSymbols = symbols;
#endif
#pragma warning disable CS0618 // Type or member is obsolete
                        EditorUserBuildSettings.androidCreateSymbolsZip = true;
#pragma warning restore CS0618 // Type or member is obsolete
#endif // UNITY_6000_0_OR_NEWER
                        break;
                    case "-versionCode":
                        BuildNumber = arguments[++i];
                        break;
#if UNITY_2017_4_OR_NEWER
                    case "-targetArchitectures":
                        var targetArchitecturesString = arguments[++i];

                        AndroidArchitecture targetArchitectures;
                        if (EnumEx.TryParse(targetArchitecturesString, true, out targetArchitectures))
                        {
                            PlayerSettings.Android.targetArchitectures = targetArchitectures;
                        }
                        else
                        {
                            Debug.LogError(string.Format("Failed to parse -targetArchitectures: \"{0}\"", targetArchitecturesString));
                        }
                        break;
#endif
                }
            }

#if UNITY_2019_1_OR_NEWER
            if (useCustomKeystore)
            {
                PlayerSettings.Android.useCustomKeystore = true;
            }
#endif
        }

        /// <inheritdoc />
        public override void OnPreProcessBuild(BuildReport report)
        {
            base.OnPreProcessBuild(report);

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget)
            {
                return;
            }

            if (ApplicationEx.isBatchMode)
            {
                // Disable to prevent gradle form killing parallel builds on same build machine
                EditorPrefs.SetBool("AndroidGradleStopDaemonsOnExit", false);
            }
        }
    }
}
