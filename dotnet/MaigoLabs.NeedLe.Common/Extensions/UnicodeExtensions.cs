using System.Text;

namespace MaigoLabs.NeedLe.Common.Extensions;

public static class UnicodeExtensions
{
    public static IEnumerable<int> ToCodePoints(this string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            int codePoint = char.ConvertToUtf32(s, i);
            if (codePoint > 0xffff) i++;
            yield return codePoint;
        }
    }

    public static StringBuilder ToUtf32StringBuilder(this IEnumerable<int> codePoints)
    {
        var sb = new StringBuilder();
        foreach (var codePoint in codePoints) sb.Append(char.ConvertFromUtf32(codePoint));
        return sb;
    }

    public static string ToUtf32String(this IEnumerable<int> codePoints) => ToUtf32StringBuilder(codePoints).ToString();
}
