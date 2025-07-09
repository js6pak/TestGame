using UnityEditor.Build.Reporting;

namespace UnityEditor
{
    public static class BuildPipelineEx
    {
        public static BuildReport BuildPlayer(EditorBuildSettingsScene[] levels, string locationPathName, BuildTarget target, BuildOptions options)
        {
#if UNITY_2018_1_OR_NEWER
            return BuildPipeline.BuildPlayer(levels, locationPathName, target, options);
#else
            return new BuildReport(BuildPipeline.BuildPlayer(levels, locationPathName, target, options));
#endif
        }
    }
}
