namespace System
{
    public static class EnumEx
    {
        public static bool TryParse<TEnum>(string value, out TEnum result) where TEnum : struct
        {
            return TryParse(value, false, out result);
        }

        public static bool TryParse<TEnum>(string value, bool ignoreCase, out TEnum result) where TEnum : struct
        {
#if NET40_OR_GREATER
            return Enum.TryParse(value, ignoreCase, out result);
#else
            try
            {
                result = (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase);
                return true;
            }
            catch
            {
                result = default(TEnum);
                return false;
            }
#endif
        }
    }
}
