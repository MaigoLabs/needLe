export const normalizeByCodePoint = (string: string) => [...string].map(normalizeCodePoint).join('');

export const normalizeCodePoint = (char: string) => {
  const codePoint = char.codePointAt(0)!;
  // Fullwidth ASCII -> Halfwidth ASCII
  if (codePoint >= 0xFF01 && codePoint <= 0xFF5E) return String.fromCodePoint(codePoint - 0xFEE0).toLowerCase();
  // Fullwidth space -> Halfwidth space
  else if (codePoint === /* '　' */ 0x3000) return ' ';
  // Halfwidth kana (U+FF66 - U+FF9D) -> Fullwidth kana
  else if (codePoint >= 0xFF66 && codePoint <= 0xFF9D) return HALF_TO_FULL_KANA[char] ?? char;
  else if (codePoint === /* '｡' */ 0xFF61) return '。';
  else if (codePoint === /* '｢' */ 0xFF62) return '「';
  else if (codePoint === /* '｣' */ 0xFF63) return '」';
  else if (codePoint === /* '､' */ 0xFF64) return '、';
  else if (codePoint === /* '･' */ 0xFF65) return '・';
  else if (codePoint === /* 'ﾞ' */ 0xFF9E || codePoint === /* '゛' */ 0x309B) return '\u3099'; // -> COMBINING KATAKANA-HIRAGANA VOICED SOUND MARK
  else if (codePoint === /* 'ﾟ' */ 0xFF9F || codePoint === /* '゜' */ 0x309C) return '\u309A'; // -> COMBINING KATAKANA-HIRAGANA SEMI-VOICED SOUND MARK
  else return char.toLowerCase();
};

const HALF_TO_FULL_KANA: Record<string, string> = {
  'ｦ': 'ヲ', 'ｧ': 'ァ', 'ｨ': 'ィ', 'ｩ': 'ゥ', 'ｪ': 'ェ', 'ｫ': 'ォ',
  'ｬ': 'ャ', 'ｭ': 'ュ', 'ｮ': 'ョ', 'ｯ': 'ッ',
  'ｰ': 'ー',
  'ｱ': 'ア', 'ｲ': 'イ', 'ｳ': 'ウ', 'ｴ': 'エ', 'ｵ': 'オ',
  'ｶ': 'カ', 'ｷ': 'キ', 'ｸ': 'ク', 'ｹ': 'ケ', 'ｺ': 'コ',
  'ｻ': 'サ', 'ｼ': 'シ', 'ｽ': 'ス', 'ｾ': 'セ', 'ｿ': 'ソ',
  'ﾀ': 'タ', 'ﾁ': 'チ', 'ﾂ': 'ツ', 'ﾃ': 'テ', 'ﾄ': 'ト',
  'ﾅ': 'ナ', 'ﾆ': 'ニ', 'ﾇ': 'ヌ', 'ﾈ': 'ネ', 'ﾉ': 'ノ',
  'ﾊ': 'ハ', 'ﾋ': 'ヒ', 'ﾌ': 'フ', 'ﾍ': 'ヘ', 'ﾎ': 'ホ',
  'ﾏ': 'マ', 'ﾐ': 'ミ', 'ﾑ': 'ム', 'ﾒ': 'メ', 'ﾓ': 'モ',
  'ﾔ': 'ヤ', 'ﾕ': 'ユ', 'ﾖ': 'ヨ',
  'ﾗ': 'ラ', 'ﾘ': 'リ', 'ﾙ': 'ル', 'ﾚ': 'レ', 'ﾛ': 'ロ',
  'ﾜ': 'ワ', 'ﾝ': 'ン',
};

const isHiraganaRange = (charCode: number) => (charCode >= 0x3041 && charCode <= 0x3096) || (charCode >= 0x309D && charCode <= 0x309E);
export const toKatakanaSingle = (char: string) => {
  const code = char.charCodeAt(0);
  return isHiraganaRange(code) ? String.fromCharCode(code + 0x60) : char;
};
export const toKatakana = (string: string) => [...string].map(toKatakanaSingle).join('');
