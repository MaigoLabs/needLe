import type { TokenizerBuilder } from '@patdx/kuromoji';

import { getHanVariants, getPinyinCandidates } from './han';
import { createKanaTranscriptionEnumerator, createRomajiTranscriptionEnumerator, isMaybeJapanese } from './japanese';
import { normalizeByCodePoint } from '../common/normalize';
import { TokenType, type TokenDefinition } from '../common/types';

export interface Token {
  id: number;
  start: number;
  end: number;
}

export type KuromojiTokenizer = Awaited<ReturnType<TokenizerBuilder['build']>>;
export interface TokenizerOptions {
  kuromoji: KuromojiTokenizer;
}
export const createTokenizer = (options: TokenizerOptions) => {
  const tokens = new Map<string, TokenDefinition>();
  let nextId = 0;
  const ensureToken = (type: TokenType, text: string) => {
    const key = `${type}:${text}`;
    let tokenDefinition = tokens.get(key);
    if (tokenDefinition) return tokenDefinition;
    tokenDefinition = { id: nextId++, type, text, codePointLength: [...text].length };
    tokens.set(key, tokenDefinition);
    return tokenDefinition;
  };

  const enumerateAllKanaCombinations = createKanaTranscriptionEnumerator(options.kuromoji);
  const enumerateAllRomajiCombinations = createRomajiTranscriptionEnumerator(options.kuromoji);
  const tokenize = (text: string) => {
    const results: Token[] = [];
    const emitter = (start: number, end: number) => (type: TokenType, text: string) => results.push({ id: ensureToken(type, text).id, start, end });

    const emitMaybeJapanese = (codePoints: string[], offset: number) => {
      for (const { start, length, transcriptions } of enumerateAllKanaCombinations(codePoints)) {
        const emit = emitter(offset + start, offset + start + length);
        for (const transcription of transcriptions) emit(TokenType.Kana, transcription);
      }
      for (const { start, length, transcriptions } of enumerateAllRomajiCombinations(codePoints)) {
        const emit = emitter(offset + start, offset + start + length);
        for (const transcription of transcriptions) emit(TokenType.Romaji, transcription);
      }
      for (let i = 0; i < codePoints.length; i++) {
        // Single character may have not only kana readings, but also Chinese pronunciations or Simplified/Traditional/Japanese variants.
        const character = codePoints[i]!;
        const hanAlternates = getHanVariants(character); // All possible variant characters (Simplified/Traditional/Japanese)
        const pinyinAlternates = Array.from(new Set(hanAlternates.flatMap(han => getPinyinCandidates(han)))); // All possible pinyin candidates
        const emit = emitter(offset + i, offset + i + 1);
        for (const han of hanAlternates) emit(TokenType.Han, han);
        for (const pinyin of pinyinAlternates) emit(TokenType.Pinyin, pinyin);
      }
    };
    const emitRaw = (codePoint: string, offset: number) => emitter(offset, offset + 1)(TokenType.Raw, codePoint);

    const codePoints = [...normalizeByCodePoint(text)];
    for (let start = 0; start < codePoints.length;) {
      const codePoint = codePoints[start]!;

      const consequentCharsets = [
        { is: isMaybeJapanese, emit: emitMaybeJapanese },
      ];
      let emitted = false;
      for (const { is, emit } of consequentCharsets) {
        let length = 0;
        while (start + length < codePoints.length && is(codePoints[start + length]!)) length++;
        if (length > 0) {
          emit(codePoints.slice(start, start + length), start);
          start += length;
          emitted = true;
          break;
        }
      }
      if (emitted) continue;

      // Skip whitespaces
      if (/\s/.test(codePoint)) {
        start++;
        continue;
      }

      emitRaw(codePoint, start);
      start++;
    }
    return results;
  };

  return {
    tokens,
    tokenize,
  };
};
