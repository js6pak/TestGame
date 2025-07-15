#if UNITY_5_3_OR_NEWER
namespace UnityEngine
{
    public static class DebugEx
    {
        public static ILogger unityLogger
        {
            get
            {
#if UNITY_2017_1_OR_NEWER
                return Debug.unityLogger;
#else
                return Debug.logger;
#endif
            }
        }
    }
}
#endif
