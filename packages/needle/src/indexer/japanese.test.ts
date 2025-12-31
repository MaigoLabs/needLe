import path from 'node:path';
import url from 'node:url';

import { TokenizerBuilder } from '@patdx/kuromoji';
import NodeDictionaryLoader from '@patdx/kuromoji/node';

import { getAllKanaReadings, toRomajiStrictly } from './japanese';
import type { KuromojiTokenizer } from './tokenizer';

let kuromoji: KuromojiTokenizer;

beforeAll(async () => {
  const kuromojiDictPath = path.resolve(url.fileURLToPath(import.meta.resolve('@patdx/kuromoji')), '..', '..', 'dict');
  kuromoji = await new TokenizerBuilder({ loader: new NodeDictionaryLoader({ dic_path: kuromojiDictPath }) }).build();
});

describe('toRomajiStrictly', () => {
  it('should convert basic kana to romaji', () => {
    expect(toRomajiStrictly('あ')).toBe('a');
    expect(toRomajiStrictly('か')).toBe('ka');
    expect(toRomajiStrictly('さくら')).toBe('sakura');
  });

  it('should convert katakana to romaji', () => {
    expect(toRomajiStrictly('ア')).toBe('a');
    expect(toRomajiStrictly('カ')).toBe('ka');
    expect(toRomajiStrictly('サクラ')).toBe('sakura');
  });

  it('should handle long vowels', () => {
    expect(toRomajiStrictly('おう')).toBe('ou');
    expect(toRomajiStrictly('おお')).toBe('oo');
  });

  it('should return empty string for invalid first character', () => {
    expect(toRomajiStrictly('ー')).toBe(''); // prolonged sound mark cannot be first
    expect(toRomajiStrictly('ゃ')).toBe(''); // small ya cannot be first
  });

  it('should return empty string for invalid last character', () => {
    expect(toRomajiStrictly('っ')).toBe(''); // small tsu cannot be last
  });

  it('should handle gemination (small tsu)', () => {
    expect(toRomajiStrictly('かった')).toBe('katta');
  });
});

describe('getAllKanaReadings', () => {
  it('should return katakana reading for pure kana input', () => {
    const readings = getAllKanaReadings(kuromoji, 'あ');
    expect(readings).toContain('ア');
  });

  it('should return readings for kanji', () => {
    const readings = getAllKanaReadings(kuromoji, '僕');
    expect(readings.length).toBeGreaterThan(0);
    // 僕 should have reading ボク
    expect(readings).toContain('ボク');
  });

  it('should return readings for compound words', () => {
    const readings = getAllKanaReadings(kuromoji, '和風');
    expect(readings.length).toBeGreaterThan(0);
  });
});
