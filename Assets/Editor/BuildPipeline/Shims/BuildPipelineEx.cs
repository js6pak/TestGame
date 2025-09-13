using System.Reflection;
using System.Linq;
using UnityEditor.Build.Reporting;

namespace UnityEditor
{
    public static class BuildPipelineEx
    {
        public static BuildReport BuildPlayer(EditorBuildSettingsScene[] levels, string locationPathName, BuildTarget target, BuildOptions options)
        {
#if UNITY_2018_1_OR_NEWER
            return BuildPipeline.BuildPlayer(levels, locationPathName, target, options);
#elif UNITY_5_5_OR_NEWER
            return new BuildReport(BuildPipeline.BuildPlayer(levels, locationPathName, target, options));
#else
            return new BuildReport(BuildPipeline.BuildPlayer(levels.Select(l => l.path).ToArray(), locationPathName, target, options));
#endif
        }

#if !UNITY_5_6_OR_NEWER
        private static readonly MethodInfo s_GetBuildTargetGroup = typeof(BuildPipeline).GetMethod("GetBuildTargetGroup", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
#endif

        public static BuildTargetGroup GetBuildTargetGroup(BuildTarget platform)
        {
#if UNITY_5_6_OR_NEWER
            return BuildPipeline.GetBuildTargetGroup(platform);
#else
            return (BuildTargetGroup)s_GetBuildTargetGroup.Invoke(null, new object[] { platform });
#endif
        }
    }
}
