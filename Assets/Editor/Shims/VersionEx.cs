using JetBrains.Annotations;

namespace System
{
    public static class VersionEx
    {
        [ContractAnnotation("=> true, input: notnull, result: notnull; => false, result: null")]
        public static bool TryParse(string input, out Version result)
        {
#if NET40_OR_GREATER
            return Version.TryParse(input, out result);
#else
            try
            {
                result = new Version(input);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
#endif
        }
    }
}
