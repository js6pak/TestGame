using JetBrains.Annotations;

namespace System
{
    public static class StringEx
    {
        [ContractAnnotation("value:null => true")]
        public static bool IsNullOrWhiteSpace(string value)
        {
#if NET40_OR_GREATER
            return string.IsNullOrWhiteSpace(value);
#else
            if (value == null) return true;

            foreach (var c in value)
            {
                if (!char.IsWhiteSpace(c)) return false;
            }

            return true;
#endif
        }
    }
}
