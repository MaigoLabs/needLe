import path from 'node:path';
import url from 'node:url';

import { TokenizerBuilder } from '@patdx/kuromoji';
import NodeDictionaryLoader from '@patdx/kuromoji/node';

import { createTokenizer, type KuromojiTokenizer } from './tokenizer';
import { TokenType } from '../common/types';

let kuromoji: KuromojiTokenizer;

beforeAll(async () => {
  const kuromojiDictPath = path.resolve(url.fileURLToPath(import.meta.resolve('@patdx/kuromoji')), '..', '..', 'dict');
  kuromoji = await new TokenizerBuilder({ loader: new NodeDictionaryLoader({ dic_path: kuromojiDictPath }) }).build();
});

describe('tokenizer', () => {
  it('should tokenize mixed Japanese text', () => {
    const tokenizer = createTokenizer({ kuromoji });
    const tokens = tokenizer.tokenize('僕の和風本当上手');

    // Get all token definitions
    const tokenDefs = [...tokenizer.tokens.values()];

    // Should have tokens of various types
    const types = new Set(tokenDefs.map(t => t.type));
    expect(types.has(TokenType.Han)).toBe(true);
    expect(types.has(TokenType.Pinyin)).toBe(true);
    expect(types.has(TokenType.Kana)).toBe(true);
    expect(types.has(TokenType.Romaji)).toBe(true);

    const getTokenTextsAt = (pos: number, type: TokenType) => tokens
      .filter(t => t.start <= pos && t.end > pos && tokenDefs.find(d => d.id === t.id)?.type === type)
      .map(t => tokenDefs.find(d => d.id === t.id)!.text);

    // Position 0: 僕
    expect(getTokenTextsAt(0, TokenType.Han)).toContain('僕');
    expect(getTokenTextsAt(0, TokenType.Pinyin)).toContain('pu');
    expect(getTokenTextsAt(0, TokenType.Kana)).toContain('ボク');
    expect(getTokenTextsAt(0, TokenType.Romaji)).toContain('boku');

    // Position 1: の (hiragana, no Han/Pinyin)
    expect(getTokenTextsAt(1, TokenType.Han)).toEqual([]);
    expect(getTokenTextsAt(1, TokenType.Pinyin)).toEqual([]);
    expect(getTokenTextsAt(1, TokenType.Kana)).toContain('ノ');
    expect(getTokenTextsAt(1, TokenType.Romaji)).toContain('no');

    // Position 2: 和
    expect(getTokenTextsAt(2, TokenType.Han)).toContain('和');
    expect(getTokenTextsAt(2, TokenType.Pinyin)).toContain('he');
    expect(getTokenTextsAt(2, TokenType.Kana)).toContain('ワ');
    expect(getTokenTextsAt(2, TokenType.Romaji)).toContain('wa');

    // Position 3: 風
    expect(getTokenTextsAt(3, TokenType.Han)).toContain('風');
    expect(getTokenTextsAt(3, TokenType.Han)).toContain('风'); // simplified variant
    expect(getTokenTextsAt(3, TokenType.Pinyin)).toContain('feng');
    expect(getTokenTextsAt(3, TokenType.Kana)).toContain('フウ');
    expect(getTokenTextsAt(3, TokenType.Romaji)).toContain('fu');

    // Position 4: 本
    expect(getTokenTextsAt(4, TokenType.Han)).toContain('本');
    expect(getTokenTextsAt(4, TokenType.Pinyin)).toContain('ben');
    expect(getTokenTextsAt(4, TokenType.Kana)).toContain('ホン');
    expect(getTokenTextsAt(4, TokenType.Romaji)).toContain('hon');

    // Position 5: 当
    expect(getTokenTextsAt(5, TokenType.Han)).toContain('当');
    expect(getTokenTextsAt(5, TokenType.Han)).toContain('當'); // traditional variant
    expect(getTokenTextsAt(5, TokenType.Pinyin)).toContain('dang');
    expect(getTokenTextsAt(5, TokenType.Kana)).toContain('トウ');
    expect(getTokenTextsAt(5, TokenType.Romaji)).toContain('to'); // normalized: tou -> to

    // Position 6: 上
    expect(getTokenTextsAt(6, TokenType.Han)).toContain('上');
    expect(getTokenTextsAt(6, TokenType.Pinyin)).toContain('shang');
    expect(getTokenTextsAt(6, TokenType.Kana)).toContain('ジョウ');
    expect(getTokenTextsAt(6, TokenType.Romaji)).toContain('jo'); // normalized: jou -> jo

    // Position 7: 手
    expect(getTokenTextsAt(7, TokenType.Han)).toContain('手');
    expect(getTokenTextsAt(7, TokenType.Pinyin)).toContain('shou');
    expect(getTokenTextsAt(7, TokenType.Kana)).toContain('シュ');
    expect(getTokenTextsAt(7, TokenType.Romaji)).toContain('shu');

    // Check that tokens cover the entire input
    expect(tokens.length).toBeGreaterThan(0);

    // Check some specific token definitions exist
    const hanTokenTexts = tokenDefs.filter(t => t.type === TokenType.Han).map(t => t.text);
    expect(hanTokenTexts).toContain('僕');
    expect(hanTokenTexts).toContain('和');
    expect(hanTokenTexts).toContain('風');

    // Check kana readings exist for kanji
    const kanaTokenTexts = tokenDefs.filter(t => t.type === TokenType.Kana).map(t => t.text);
    expect(kanaTokenTexts).toContain('ボク'); // 僕 -> ボク

    // Check romaji readings exist
    const romajiTokenTexts = tokenDefs.filter(t => t.type === TokenType.Romaji).map(t => t.text);
    expect(romajiTokenTexts).toContain('boku'); // 僕 -> boku
  });

  it('should not create duplicate tokens when tokenizing multiple documents', () => {
    const tokenizer = createTokenizer({ kuromoji });

    // Tokenize multiple music names that share some characters
    tokenizer.tokenize('僕の和風本当上手');
    tokenizer.tokenize('僕');
    tokenizer.tokenize('和風');

    // Check that there are no duplicate tokens
    const tokenDefs = [...tokenizer.tokens.values()];
    const tokenKeys = tokenDefs.map(t => `${t.type}:${t.text}`);
    const uniqueKeys = new Set(tokenKeys);

    expect(tokenKeys.length).toBe(uniqueKeys.size);

    // Also check that IDs are unique
    const ids = tokenDefs.map(t => t.id);
    const uniqueIds = new Set(ids);
    expect(ids.length).toBe(uniqueIds.size);
  });

  it('should handle Raw tokens for non-CJK characters', () => {
    const tokenizer = createTokenizer({ kuromoji });
    tokenizer.tokenize('a-b');

    const tokenDefs = [...tokenizer.tokens.values()];
    const rawTokenTexts = tokenDefs.filter(t => t.type === TokenType.Raw).map(t => t.text);

    expect(rawTokenTexts).toContain('a'); // normalized to lowercase
    expect(rawTokenTexts).toContain('-');
    expect(rawTokenTexts).toContain('b');
  });

  it('should tokenize compound word "今日" with both individual and combined readings', () => {
    const tokenizer = createTokenizer({ kuromoji });
    const tokens = tokenizer.tokenize('今日');
    const tokenDefs = [...tokenizer.tokens.values()];

    const getTokensWithSpan = (type: TokenType, start: number, end: number) => tokens
      .filter(t => t.start === start && t.end === end && tokenDefs.find(d => d.id === t.id)?.type === type)
      .map(t => tokenDefs.find(d => d.id === t.id)!.text);

    // Individual character readings at position 0: 今
    expect(getTokensWithSpan(TokenType.Han, 0, 1)).toContain('今');
    expect(getTokensWithSpan(TokenType.Pinyin, 0, 1)).toContain('jin');
    expect(getTokensWithSpan(TokenType.Kana, 0, 1)).toContain('コン');
    expect(getTokensWithSpan(TokenType.Kana, 0, 1)).toContain('イマ');
    expect(getTokensWithSpan(TokenType.Romaji, 0, 1)).toContain('kon');
    expect(getTokensWithSpan(TokenType.Romaji, 0, 1)).toContain('ima');

    // Individual character readings at position 1: 日
    expect(getTokensWithSpan(TokenType.Han, 1, 2)).toContain('日');
    expect(getTokensWithSpan(TokenType.Pinyin, 1, 2)).toContain('ri');
    expect(getTokensWithSpan(TokenType.Kana, 1, 2)).toContain('ニチ');
    expect(getTokensWithSpan(TokenType.Kana, 1, 2)).toContain('ヒ');
    expect(getTokensWithSpan(TokenType.Romaji, 1, 2)).toContain('niti');
    expect(getTokensWithSpan(TokenType.Romaji, 1, 2)).toContain('hi');

    // Combined reading for "今日" [0, 2] - this is an indivisible compound word
    expect(getTokensWithSpan(TokenType.Kana, 0, 2)).toContain('キョウ');
    expect(getTokensWithSpan(TokenType.Romaji, 0, 2)).toContain('kyo'); // normalized: kyou -> kyo
  });
});
