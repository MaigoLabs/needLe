using hyjiacan.py4n;

namespace MaigoLabs.NeedLe.Indexer.Han;

public static class PinyinHelper
{
    private static readonly string[] PINYIN_INITIALS = ["b", "p", "m", "f", "d", "t", "n", "l", "g", "k", "h", "j", "q", "x", "zh", "ch", "sh", "r", "z", "c", "s", "y", "w"];
    private static readonly Dictionary<string, string> PINYIN_FINALS_FUZZY_MAP = new() { ["ang"] = "an", ["eng"] = "en", ["ing"] = "in" };

    public static IEnumerable<string> GetPinyinCandidates(int codePoint) => codePoint < char.MinValue || codePoint > char.MaxValue || !PinyinUtil.IsHanzi((char)codePoint) ? [] :
        Pinyin4Net.GetPinyin((char)codePoint, PinyinFormat.LOWERCASE | PinyinFormat.WITHOUT_TONE).Where(pinyin => pinyin.Length > 0).SelectMany(pinyin =>
        {
            var initial = PINYIN_INITIALS.FirstOrDefault(initial => pinyin.StartsWith(initial));
            var initialAlphabet = initial != null ? initial[..1] : pinyin[..1];
            var fuzzySuffix = pinyin.Length < 3 ? null : pinyin[^3..];
            var fuzzyPinyin = fuzzySuffix != null && PINYIN_FINALS_FUZZY_MAP.TryGetValue(fuzzySuffix, out var fuzzySuffixTarget) ? pinyin[..^3] + fuzzySuffixTarget : null;
            return new string?[] { pinyin, initial, initialAlphabet, fuzzyPinyin }.OfType<string>();
        }).Distinct();
}
