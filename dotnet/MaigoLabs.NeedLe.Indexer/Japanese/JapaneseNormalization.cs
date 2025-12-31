using MaigoLabs.NeedLe.Common.Extensions;

namespace MaigoLabs.NeedLe.Indexer.Japanese;

public static class JapaneseNormalization
{
    public delegate string Normalizer(string text);

    public static Normalizer CreateNormalizer(Dictionary<string, string> rules) => text =>
    {
        while (true)
        {
            var beforeCurrentIteration = text;
            foreach (var (from, to) in rules) text = text.Replace(from, to);
            if (text == beforeCurrentIteration) break;
        }
        return text;
    };

    public static IEnumerable<(int[] From, int[] To)> ToCodePointPairs(Dictionary<string, string> rules) =>
        rules.Select(rule => (From: rule.Key.ToCodePoints().ToArray(), To: rule.Value.ToCodePoints().ToArray()));

    public static readonly Dictionary<string, string> NORMALIZE_RULES_ROMAJI = new()
    {
        // Remove all long vowels (sa-ba- -> saba)
        ["-"] = "",
        // Collapse consecutive vowels
        ["aa"] = "a",
        ["ii"] = "i",
        ["uu"] = "u",
        ["ee"] = "e",
        ["oo"] = "o",
        ["ou"] = "o",
        // mb/mp/mm -> nb/np/nm (shimbun -> shinbun)
        ["mb"] = "nb",
        ["mp"] = "np",
        ["mm"] = "nm",
        // Others
        ["sha"] = "sya",
        ["tsu"] = "tu",
        ["chi"] = "ti",
        ["shi"] = "si",
        ["ji"] = "zi",
    };
    public static readonly IEnumerable<(int[] From, int[] To)> NORMALIZE_RULES_ROMAJI_CODEPOINTS = ToCodePointPairs(NORMALIZE_RULES_ROMAJI);
    public static readonly Normalizer NormalizeRomaji = CreateNormalizer(NORMALIZE_RULES_ROMAJI);

    public static readonly Dictionary<string, string> NORMALIZE_RULES_KANA_DAKUTEN = new()
    {
        ["う\u3099"] = "ゔ",
        ["か\u3099"] = "が", ["き\u3099"] = "ぎ", ["く\u3099"] = "ぐ", ["け\u3099"] = "げ", ["こ\u3099"] = "ご",
        ["さ\u3099"] = "ざ", ["し\u3099"] = "じ", ["す\u3099"] = "ず", ["せ\u3099"] = "ぜ", ["そ\u3099"] = "ぞ",
        ["た\u3099"] = "だ", ["ち\u3099"] = "ぢ", ["つ\u3099"] = "づ", ["て\u3099"] = "で", ["と\u3099"] = "ど",
        ["は\u3099"] = "ば", ["ひ\u3099"] = "び", ["ふ\u3099"] = "ぶ", ["へ\u3099"] = "べ", ["ほ\u3099"] = "ぼ",
        ["は\u309A"] = "ぱ", ["ひ\u309A"] = "ぴ", ["ふ\u309A"] = "ぷ", ["へ\u309A"] = "ぺ", ["ほ\u309A"] = "ぽ",
        ["ゝ\u3099"] = "ゞ",

        ["ウ\u3099"] = "ヴ",
        ["カ\u3099"] = "ガ", ["キ\u3099"] = "ギ", ["ク\u3099"] = "グ", ["ケ\u3099"] = "ゲ", ["コ\u3099"] = "ゴ",
        ["サ\u3099"] = "ザ", ["シ\u3099"] = "ジ", ["ス\u3099"] = "ズ", ["セ\u3099"] = "ゼ", ["ソ\u3099"] = "ゾ",
        ["タ\u3099"] = "ダ", ["チ\u3099"] = "ヂ", ["ツ\u3099"] = "ヅ", ["テ\u3099"] = "デ", ["ト\u3099"] = "ド",
        ["ハ\u3099"] = "バ", ["ヒ\u3099"] = "ビ", ["フ\u3099"] = "ブ", ["ヘ\u3099"] = "ベ", ["ホ\u3099"] = "ボ",
        ["ハ\u309A"] = "パ", ["ヒ\u309A"] = "ピ", ["フ\u309A"] = "プ", ["ヘ\u309A"] = "ペ", ["ホ\u309A"] = "ポ",
        ["ワ\u3099"] = "ヷ", ["ヰ\u3099"] = "ヸ", ["ヱ\u3099"] = "ヹ", ["ヲ\u3099"] = "ヺ",
        ["ヽ\u3099"] = "ヾ",
    };
    public static readonly IEnumerable<(int[] From, int[] To)> NORMALIZE_RULES_KANA_DAKUTEN_CODEPOINTS = ToCodePointPairs(NORMALIZE_RULES_KANA_DAKUTEN);
    public static readonly Normalizer NormalizeKanaDakuten = CreateNormalizer(NORMALIZE_RULES_KANA_DAKUTEN);
}
