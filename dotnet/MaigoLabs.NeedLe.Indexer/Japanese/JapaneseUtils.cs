using MaigoLabs.NeedLe.Indexer.Han;
using MyNihongo.KanaConverter;

namespace MaigoLabs.NeedLe.Indexer.Japanese;

public static class JapaneseUtils
{
    public static bool IsMaybeJapanese(int codePoint) =>
        HanVariantProvider.IsHanCharacter(codePoint) ||
        IsKana(codePoint) ||
        IsJapaneseSoundMark(codePoint) ||
        codePoint == 0x3005 || codePoint == 0x3006 || codePoint == 0x30FC;

    // See also Common/Normalization.cs
    public static bool IsJapaneseSoundMark(int codePoint) => codePoint == 0x3099 || codePoint == 0x309A;
    public static string StripJapaneseSoundMarks(string text) => string.Concat(text.Where(codePoint => !IsJapaneseSoundMark(codePoint)));

    public static bool IsKana(int codePoint) => (codePoint >= 0x3041 && codePoint <= 0x309F) || (codePoint >= 0x30A0 && codePoint <= 0x30FF);

    private static readonly int[] KANAS_CANNOT_BE_FIRST =
    [
        'ァ', 'ィ', 'ゥ', 'ェ', 'ォ',
        'ぁ', 'ぃ', 'ぅ', 'ぇ', 'ぉ',
        'ャ', 'ュ', 'ョ',
        'ゃ', 'ゅ', 'ょ',
        'ヮ', 'ゎ',
        'ㇰ', 'ㇱ', 'ㇲ', 'ㇳ', 'ㇴ', 'ㇵ', 'ㇶ', 'ㇷ', 'ㇸ', 'ㇹ', 'ㇺ', 'ㇻ', 'ㇼ', 'ㇽ', 'ㇾ', 'ㇿ',
        'ー',
    ];

    private static readonly int[] KANAS_CANNOT_BE_LAST =
    [
        'ッ', 'っ'
    ];

    public static string ToRomajiStrictly(string kanaText)
    {
        if (kanaText.Length == 0) return "";
        if (KANAS_CANNOT_BE_FIRST.Contains(kanaText[0])) return "";
        if (KANAS_CANNOT_BE_LAST.Contains(kanaText[^1])) return "";
        string romaji;
        try { romaji = kanaText.ToRomaji(); }
        catch { return ""; }
        if (!romaji.All(c => c is >= 'a' and <= 'z')) return "";
        return romaji;
    }

    public static bool IsValidJapanesePhrase(ReadOnlySpan<int> codePoints, int start, int length) =>
        // Skip splittings that cause sound marks to occur in the first position of a phrase
        !IsJapaneseSoundMark(codePoints[start]) && (start + length == codePoints.Length || !IsJapaneseSoundMark(codePoints[start + length]));
    public static bool IsValidJapanesePhrase(ReadOnlyMemory<int> codePoints, int start, int length) => IsValidJapanesePhrase(codePoints.Span, start, length);
}
