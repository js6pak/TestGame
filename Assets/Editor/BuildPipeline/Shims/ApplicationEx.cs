using System.Reflection;

namespace UnityEngine
{
    public static class ApplicationEx
    {
#if !UNITY_2018_2_OR_NEWER
        private static readonly PropertyInfo s_isBatchmode = typeof(Application).GetProperty("isBatchmode", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
#endif

        public static bool isBatchMode
        {
            get
            {
#if UNITY_2018_2_OR_NEWER
                return Application.isBatchMode;
#else
                return (bool)s_isBatchmode.GetValue(null, new object[0]);
#endif
            }
        }
    }
}
