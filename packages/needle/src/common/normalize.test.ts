import { normalizeByCodePoint, toKatakana } from './normalize';

describe('toKatakana', () => {
  it('should convert hiragana to katakana', () => {
    expect(toKatakana('あいうえお')).toBe('アイウエオ');
    expect(toKatakana('かきくけこ')).toBe('カキクケコ');
    expect(toKatakana('さしすせそ')).toBe('サシスセソ');
  });

  it('should keep katakana unchanged', () => {
    expect(toKatakana('アイウエオ')).toBe('アイウエオ');
  });

  it('should keep non-kana characters unchanged', () => {
    expect(toKatakana('abc123')).toBe('abc123');
    expect(toKatakana('漢字')).toBe('漢字');
  });

  it('should handle mixed input', () => {
    expect(toKatakana('あアa漢')).toBe('アアa漢');
  });
});

describe('normalizeByCodePoint', () => {
  it('should convert fullwidth ASCII to halfwidth lowercase', () => {
    expect(normalizeByCodePoint('ＡＢＣ')).toBe('abc');
    expect(normalizeByCodePoint('１２３')).toBe('123');
    expect(normalizeByCodePoint('！＠＃')).toBe('!@#');
  });

  it('should convert fullwidth space to halfwidth space', () => {
    expect(normalizeByCodePoint('　')).toBe(' ');
  });

  it('should convert halfwidth kana to fullwidth kana', () => {
    expect(normalizeByCodePoint('ｱｲｳｴｵ')).toBe('アイウエオ');
    expect(normalizeByCodePoint('ｶｷｸｹｺ')).toBe('カキクケコ');
  });

  it('should normalize voiced/semi-voiced sound marks', () => {
    expect(normalizeByCodePoint('ﾞ')).toBe('\u3099'); // halfwidth voiced -> combining
    expect(normalizeByCodePoint('ﾟ')).toBe('\u309A'); // halfwidth semi-voiced -> combining
    expect(normalizeByCodePoint('゛')).toBe('\u3099'); // fullwidth voiced -> combining
    expect(normalizeByCodePoint('゜')).toBe('\u309A'); // fullwidth semi-voiced -> combining
  });

  it('should convert halfwidth punctuation to fullwidth', () => {
    expect(normalizeByCodePoint('｡')).toBe('。');
    expect(normalizeByCodePoint('｢')).toBe('「');
    expect(normalizeByCodePoint('｣')).toBe('」');
    expect(normalizeByCodePoint('､')).toBe('、');
    expect(normalizeByCodePoint('･')).toBe('・');
  });

  it('should lowercase regular ASCII', () => {
    expect(normalizeByCodePoint('ABC')).toBe('abc');
  });

  // Should keep hiragana unchanged
});
