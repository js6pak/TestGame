#if !UNITY_2018_1_OR_NEWER
using System;

namespace UnityEditor.Build.Reporting
{
    public class BuildReport
    {
        public readonly string errors;

        public BuildSummary summary;

        public BuildReport(string errors)
        {
            this.errors = errors;
            summary = new BuildSummary
            {
                result = StringEx.IsNullOrWhiteSpace(errors) ? BuildResult.Succeeded : BuildResult.Failed,
            };
        }
    }

    public struct BuildSummary
    {
        public BuildResult result;
    }

    public enum BuildResult
    {
        Unknown = 0,
        Succeeded = 1,
        Failed = 2,
        Cancelled = 3,
    }
}
#endif
