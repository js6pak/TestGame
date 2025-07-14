using System.Text;
using System.Text.RegularExpressions;

namespace GenMatrix;

internal static class Glob
{
    private static void Convert(ReadOnlySpan<char> glob, StringBuilder regex)
    {
        while (!glob.IsEmpty)
        {
            var c = glob[0];
            glob = glob[1..];

            switch (c)
            {
                case '*':
                {
                    regex.Append(".*");
                    continue;
                }

                case '?':
                {
                    regex.Append('.');
                    continue;
                }

                case '{':
                {
                    var closeBrace = glob.IndexOfUnescaped('}');
                    if (closeBrace != -1)
                    {
                        var alternatives = glob[..closeBrace];
                        glob = glob[(closeBrace + 1)..];

                        regex.Append('(');

                        var j = 0;
                        foreach (var range in alternatives.Split(','))
                        {
                            if (j > 0) regex.Append('|');
                            Convert(alternatives[range], regex);
                            j++;
                        }

                        regex.Append(')');

                        continue;
                    }

                    break;
                }
            }

            regex.Append(Regex.Escape(c.ToString()));
        }
    }

    private static string Convert(ReadOnlySpan<char> glob)
    {
        if (glob.IsEmpty)
            return "^$";

        var regex = new StringBuilder();
        regex.Append('^');

        Convert(glob, regex);

        regex.Append('$');
        return regex.ToString();
    }

    public static Regex ToRegex(ReadOnlySpan<char> glob, RegexOptions options = RegexOptions.Compiled)
    {
        return new Regex(Convert(glob), options);
    }

    private static int IndexOfUnescaped(this ReadOnlySpan<char> span, char character)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == character)
            {
                var backslashCount = 0;

                var j = i - 1;
                while (j >= 0 && span[j] == '\\')
                {
                    backslashCount++;
                    j--;
                }

                if (backslashCount % 2 == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }
}
