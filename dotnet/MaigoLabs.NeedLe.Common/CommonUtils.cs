namespace MaigoLabs.NeedLe.Common;

public static class CommonUtils
{
    public static bool IsWhitespace(int codePoint) =>
        codePoint == 0x0009 /* \t */ ||
        codePoint == 0x000A /* \n */ ||
        codePoint == 0x000B /* Vertical Tab */ ||
        codePoint == 0x000C /* \f */ ||
        codePoint == 0x000D /* \r */ ||
        codePoint == 0x0020 /* Space */ ||
        codePoint == 0x0085 /* Next Line (NEL) */ ||
        codePoint == 0x00A0 /* No-Break Space */ ||
        codePoint == 0x1680 /* Ogham Space Mark */ ||
        codePoint >= 0x2000 && codePoint <= 0x200A ||
        codePoint == 0x2028 /* Line Separator */ ||
        codePoint == 0x2029 /* Paragraph Separator */ ||
        codePoint == 0x202F /* Narrow No-Break Space */ ||
        codePoint == 0x205F /* Medium Mathematical Space */ ||
        codePoint == 0x3000 /* Ideographic Space */;
}
