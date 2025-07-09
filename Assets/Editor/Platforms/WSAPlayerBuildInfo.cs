// // Licensed under the MIT License. See LICENSE in the project root for license information.
//
// using System;
// using UnityEditor;
// using UnityEngine;
//
// namespace Buildalon.Editor.BuildPipeline
// {
//     public class WSAPlayerBuildInfo : BuildInfo
//     {
//         /// <inheritdoc />
//         public override BuildTarget BuildTarget
//         {
//             get { return BuildTarget.WSAPlayer; }
//         }
//
//         /// <inheritdoc />
//         public override BuildTargetGroup BuildTargetGroup
//         {
//             get { return BuildTargetGroup.WSA; }
//         }
//
//         /// <inheritdoc />
//         public override string FullOutputPath
//         {
//             get { return OutputDirectory; }
//         }
//
//         /// <inheritdoc />
//         public override void ParseCommandLineArgs()
//         {
//             base.ParseCommandLineArgs();
//             var arguments = Environment.GetCommandLineArgs();
//
//             for (int i = 0; i < arguments.Length; ++i)
//             {
//                 switch (arguments[i])
//                 {
//                     case "-arch":
//                         var arch = arguments[++i];
//
//                         if (!StringEx.IsNullOrWhiteSpace(arch))
//                         {
//                             EditorUserBuildSettings.wsaArchitecture = arch;
//                         }
//                         else
//                         {
//                             Debug.LogError(string.Format("Failed to parse -arch \"{0}\"", arguments[i]));
//                         }
//                         break;
// #if !UNITY_2021_1_OR_NEWER
//                     case "-wsaSubtarget":
//                         if (EnumEx.TryParse<WSASubtarget>(arguments[++i], out var subTarget))
//                         {
//                             EditorUserBuildSettings.wsaSubtarget = subTarget;
//                         }
//                         else
//                         {
//                             Debug.LogError(string.Format("Failed to parse -wsaSubtarget \"{0}\"", arguments[i]));
//                         }
//                         break;
// #endif
//                     case "-wsaUWPBuildType":
//                         if (EnumEx.TryParse<WSAUWPBuildType>(arguments[++i], out var buildType))
//                         {
//                             EditorUserBuildSettings.wsaUWPBuildType = buildType;
//                         }
//                         else
//                         {
//                             Debug.LogError(string.Format("Failed to parse -wsaUWPBuildType \"{0}\"", arguments[i]));
//                         }
//                         break;
//                     case "-wsaSetDeviceFamily":
//                         PlayerSettings.WSATargetFamily family;
//                         if (EnumEx.TryParse(arguments[++i], out family))
//                         {
//                             PlayerSettings.WSA.SetTargetDeviceFamily(family, true);
//                         }
//                         else
//                         {
//                             Debug.LogError(string.Format("Failed to parse -wsaSetDeviceFamily \"{0}\"", arguments[i]));
//                         }
//                         break;
//                     case "-wsaUWPSDK":
//                         EditorUserBuildSettings.wsaUWPSDK = arguments[++i];
//                         break;
//                     case "-wsaMinUWPSDK":
//                         EditorUserBuildSettings.wsaMinUWPSDK = arguments[++i];
//                         break;
//                     case "-wsaCertificate":
//                         var path = arguments[++i];
//
//                         if (StringEx.IsNullOrWhiteSpace(path))
//                         {
//                             Debug.LogError("Failed to parse -wsaCertificate. Missing path!");
//                             break;
//                         }
//
//                         var password = arguments[++i];
//
//                         if (StringEx.IsNullOrWhiteSpace(password))
//                         {
//                             Debug.LogError("Failed to parse -wsaCertificate. Missing password!");
//                             break;
//                         }
//
//                         PlayerSettings.WSA.SetCertificate(path, password);
//                         break;
//                 }
//             }
//         }
//     }
// }


