namespace MaigoLabs.NeedLe.Common;

// This is for global normalization for any input and documents.
public static class CommonNormalization
{
    public static int NormalizeCodePoint(int codePoint)
    {
        // Fullwidth ASCII -> Halfwidth ASCII
        if (codePoint >= 0xFF01 && codePoint <= 0xFF5E) return ToLowerCaseAscii(codePoint - 0xFEE0);
        // Fullwidth space -> Halfwidth space
        else if (codePoint == /* '　' */ 0x3000) return ' ';
        // Halfwidth kana (U+FF66 - U+FF9D) -> Fullwidth kana
        else if (codePoint >= 0xFF66 && codePoint <= 0xFF9D) return HALF_TO_FULL_KANA.TryGetValue(codePoint, out var value) ? value : codePoint;
        else if (codePoint == /* '｡' */ 0xFF61) return '。';
        else if (codePoint == /* '｢' */ 0xFF62) return '「';
        else if (codePoint == /* '｣' */ 0xFF63) return '」';
        else if (codePoint == /* '､' */ 0xFF64) return '、';
        else if (codePoint == /* '･' */ 0xFF65) return '・';
        else if (codePoint == /* 'ﾞ' */ 0xFF9E || codePoint == /* '゛' */ 0x309B) return 0x3099; // -> COMBINING KATAKANA-HIRAGANA VOICED SOUND MARK
        else if (codePoint == /* 'ﾟ' */ 0xFF9F || codePoint == /* '゜' */ 0x309C) return 0x309A; // -> COMBINING KATAKANA-HIRAGANA SEMI-VOICED SOUND MARK
        else return ToLowerCaseAscii(codePoint);
    }

    private static readonly Dictionary<int, int> HALF_TO_FULL_KANA = new Dictionary<int, int> {
        ['ｦ'] = 'ヲ', ['ｧ'] = 'ァ', ['ｨ'] = 'ィ', ['ｩ'] = 'ゥ', ['ｪ'] = 'ェ', ['ｫ'] = 'ォ',
        ['ｬ'] = 'ャ', ['ｭ'] = 'ュ', ['ｮ'] = 'ョ', ['ｯ'] = 'ッ',
        ['ｰ'] = 'ー',
        ['ｱ'] = 'ア', ['ｲ'] = 'イ', ['ｳ'] = 'ウ', ['ｴ'] = 'エ', ['ｵ'] = 'オ',
        ['ｶ'] = 'カ', ['ｷ'] = 'キ', ['ｸ'] = 'ク', ['ｹ'] = 'ケ', ['ｺ'] = 'コ',
        ['ｻ'] = 'サ', ['ｼ'] = 'シ', ['ｽ'] = 'ス', ['ｾ'] = 'セ', ['ｿ'] = 'ソ',
        ['ﾀ'] = 'タ', ['ﾁ'] = 'チ', ['ﾂ'] = 'ツ', ['ﾃ'] = 'テ', ['ﾄ'] = 'ト',
        ['ﾅ'] = 'ナ', ['ﾆ'] = 'ニ', ['ﾇ'] = 'ヌ', ['ﾈ'] = 'ネ', ['ﾉ'] = 'ノ',
        ['ﾊ'] = 'ハ', ['ﾋ'] = 'ヒ', ['ﾌ'] = 'フ', ['ﾍ'] = 'ヘ', ['ﾎ'] = 'ホ',
        ['ﾏ'] = 'マ', ['ﾐ'] = 'ミ', ['ﾑ'] = 'ム', ['ﾒ'] = 'メ', ['ﾓ'] = 'モ',
        ['ﾔ'] = 'ヤ', ['ﾕ'] = 'ユ', ['ﾖ'] = 'ヨ',
        ['ﾗ'] = 'ラ', ['ﾘ'] = 'リ', ['ﾙ'] = 'ル', ['ﾚ'] = 'レ', ['ﾛ'] = 'ロ',
        ['ﾜ'] = 'ワ', ['ﾝ'] = 'ン',
    };

    public static int ToLowerCaseAscii(int codePoint) => codePoint >= 0x41 && codePoint <= 0x5A ? codePoint + 0x20 : codePoint;

    public static bool IsHiraganaRange(int codePoint) => (codePoint >= 0x3041 && codePoint <= 0x3096) || (codePoint >= 0x309D && codePoint <= 0x309E);
    public static int ToKatakana(int codePoint) => IsHiraganaRange(codePoint) ? codePoint + 0x60 : codePoint;
    public static string ToKatakana(string text) => string.Concat(text.Select(c => (char)ToKatakana(c)));
}
