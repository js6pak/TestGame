#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
#define UNITY_5_0_OR_NEWER
#endif


namespace UnityEditor
{
    public static class BuildTargetEx
    {
        public const BuildTarget iOS =
#if UNITY_5_0_OR_NEWER
            BuildTarget.iOS;
#else
            BuildTarget.iPhone;
#endif
    }

    public static class BuildTargetGroupEx
    {
        public const BuildTargetGroup iOS =
#if UNITY_5_0_OR_NEWER
            BuildTargetGroup.iOS;
#else
            BuildTargetGroup.iPhone;
#endif
    }
}
