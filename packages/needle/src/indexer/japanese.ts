import { fromKana } from 'hepburn';

import type { KuromojiTokenizer } from './tokenizer';
import { toKatakana } from '../common';

// We have normalized all other sound marks to \u3099 and \u309A (combining kata-hiragana voiced/semi-voiced sound marks)
export const isMaybeJapanese = (phrase: string) => /^[\p{Script=Han}\u3041-\u309F\u30A0-\u30FF\u3005\u3006\u30FC\u3099\u309A]+$/u.test(phrase);

// See also normalize.ts
export const isJapaneseSoundMark = (phrase: string) => /^[\u3099\u309A]+$/.test(phrase);
export const stripJapaneseSoundMarks = (phrase: string) => phrase.replaceAll('\u3099', '').replaceAll('\u309A', '');

export const isKanaSingle = (char: string) => {
  const code = char.charCodeAt(0);
  return (code >= 0x3041 && code <= 0x309F) || (code >= 0x30A0 && code <= 0x30FF);
};
export const isKana = (phrase: string) => [...phrase].every(isKanaSingle);

const KANAS_CANNOT_BE_FIRST = [
  'ァ', 'ィ', 'ゥ', 'ェ', 'ォ',
  'ぁ', 'ぃ', 'ぅ', 'ぇ', 'ぉ',
  'ャ', 'ュ', 'ョ',
  'ゃ', 'ゅ', 'ょ',
  'ヮ', 'ゎ',
  'ㇰ', 'ㇱ', 'ㇲ', 'ㇳ', 'ㇴ', 'ㇵ', 'ㇶ', 'ㇷ', 'ㇸ', 'ㇹ', 'ㇺ', 'ㇻ', 'ㇼ', 'ㇽ', 'ㇾ', 'ㇿ',
  'ー',
];
const KANAS_CANNOT_BE_LAST = [
  'ッ', 'っ',
];
export const toRomajiStrictly = (kana: string) => {
  if (KANAS_CANNOT_BE_FIRST.includes(kana[0]!)) return '';
  if (KANAS_CANNOT_BE_LAST.includes(kana[kana.length - 1]!)) return '';
  const romaji = fromKana(kana).toLowerCase()
    .replaceAll('ā', 'aa')
    .replaceAll('ī', 'ii')
    .replaceAll('ū', 'uu')
    .replaceAll('ē', 'ee')
    .replaceAll('ō', 'ou');
  if (!romaji.match(/^[a-z]+$/)) return '';
  return romaji;
};

export const createTranscriptionEnumerator = (
  isValidPhrase: (codePoints: string[], start: number, length: number) => boolean,
  getAllTranscriptions: (phrase: string) => string[],
) => (codePoints: string[]) => {
  const toKey = (start: number, length: number) => `${start}:${length}`;
  const resultMap = new Map<string, { start: number; length: number; transcriptions: string[] }>();
  for (let phraseLength = 1; phraseLength <= codePoints.length; phraseLength++) for (let start = 0; start + phraseLength <= codePoints.length; start++) {
    if (!isValidPhrase(codePoints, start, phraseLength)) continue;
    const phrase = codePoints.slice(start, start + phraseLength).join('');
    const atomicTranscriptions = [...new Set(getAllTranscriptions(phrase))].filter(candidateTranscription => {
      if (!candidateTranscription) return false;
      // Ensure the transcription is atomic (not a combination of multiple shorter transcriptions, separated by any midpoints)
      type State = { phrasePosition: number; transcriptionPosition: number };
      const toStateKey = (state: State) => `${state.phrasePosition}:${state.transcriptionPosition}`;
      const visitedStates = new Set<string>();
      const queue: State[] = [{ phrasePosition: 0, transcriptionPosition: 0 }];
      while (queue.length > 0) {
        const { phrasePosition, transcriptionPosition } = queue.shift()!;
        for (let prefixLength = 1; prefixLength <= phraseLength - phrasePosition; prefixLength++) {
          const prefixResult = resultMap.get(toKey(start + phrasePosition, prefixLength));
          if (!prefixResult) continue;
          for (const transcription of prefixResult.transcriptions) {
            if (candidateTranscription.slice(transcriptionPosition, transcriptionPosition + transcription.length) === transcription) {
              const nextState: State = { phrasePosition: phrasePosition + prefixLength, transcriptionPosition: transcriptionPosition + transcription.length };
              if (nextState.phrasePosition === phraseLength && nextState.transcriptionPosition === candidateTranscription.length) return false; // Found a valid combination
              if (visitedStates.has(toStateKey(nextState))) continue;
              visitedStates.add(toStateKey(nextState));
              queue.push(nextState);
            }
          }
        }
      }
      return true;
    });
    if (atomicTranscriptions.length > 0) resultMap.set(toKey(start, phraseLength), { start, length: phraseLength, transcriptions: atomicTranscriptions });
  }
  return [...resultMap.values()];
};

export const getAllKanaReadings = (kuromoji: KuromojiTokenizer, phrase: string) => Array.from(new Set(
  [
    ...isKana(phrase) ? [toKatakana(phrase)] : [],
    ...isKana(phrase) && [...phrase].length === 1 ? [] : ((kuromoji.token_info_dictionary.target_map[kuromoji.viterbi_builder.trie.lookup(phrase)] ?? [])
      .map(id => kuromoji.formatter.formatEntry(
        id, 0, 'KNOWN',
        kuromoji.token_info_dictionary.getFeatures(id as unknown as string)?.split(',') ?? [],
      ).reading)
      .filter((reading): reading is string => !!reading))
      .map(toKatakana),
  ],
));

const createNormalizer = (rules: Record<string, string>) => (text: string) => {
  while (true) {
    const beforeCurrentIteration = text;
    for (const [from, to] of Object.entries(rules)) text = text.replaceAll(from, to);
    if (text === beforeCurrentIteration) break;
  }
  return text;
};

export const NORMALIZE_RULES_ROMAJI: Record<string, string> = {
  // Remove all long vowels (sa-ba- -> saba)
  '-': '',
  // Collapse consecutive vowels
  'aa': 'a',
  'ii': 'i',
  'uu': 'u',
  'ee': 'e',
  'oo': 'o',
  'ou': 'o',
  // mb/mp/mm -> nb/np/nm (shimbun -> shinbun)
  'mb': 'nb',
  'mp': 'np',
  'mm': 'nm',
  // Others
  'sha': 'sya',
  'tsu': 'tu',
  'chi': 'ti',
  'shi': 'si',
  'ji': 'zi',
};
export const normalizeRomaji = createNormalizer(NORMALIZE_RULES_ROMAJI);

export const NORMALIZE_RULES_KANA_DAKUTEN: Record<string, string> = {
  'う\u3099': 'ゔ',
  'か\u3099': 'が', 'き\u3099': 'ぎ', 'く\u3099': 'ぐ', 'け\u3099': 'げ', 'こ\u3099': 'ご',
  'さ\u3099': 'ざ', 'し\u3099': 'じ', 'す\u3099': 'ず', 'せ\u3099': 'ぜ', 'そ\u3099': 'ぞ',
  'た\u3099': 'だ', 'ち\u3099': 'ぢ', 'つ\u3099': 'づ', 'て\u3099': 'で', 'と\u3099': 'ど',
  'は\u3099': 'ば', 'ひ\u3099': 'び', 'ふ\u3099': 'ぶ', 'へ\u3099': 'べ', 'ほ\u3099': 'ぼ',
  'は\u309A': 'ぱ', 'ひ\u309A': 'ぴ', 'ふ\u309A': 'ぷ', 'へ\u309A': 'ぺ', 'ほ\u309A': 'ぽ',
  'ゝ\u3099': 'ゞ',

  'ウ\u3099': 'ヴ',
  'カ\u3099': 'ガ', 'キ\u3099': 'ギ', 'ク\u3099': 'グ', 'ケ\u3099': 'ゲ', 'コ\u3099': 'ゴ',
  'サ\u3099': 'ザ', 'シ\u3099': 'ジ', 'ス\u3099': 'ズ', 'セ\u3099': 'ゼ', 'ソ\u3099': 'ゾ',
  'タ\u3099': 'ダ', 'チ\u3099': 'ヂ', 'ツ\u3099': 'ヅ', 'テ\u3099': 'デ', 'ト\u3099': 'ド',
  'ハ\u3099': 'バ', 'ヒ\u3099': 'ビ', 'フ\u3099': 'ブ', 'ヘ\u3099': 'ベ', 'ホ\u3099': 'ボ',
  'ハ\u309A': 'パ', 'ヒ\u309A': 'ピ', 'フ\u309A': 'プ', 'ヘ\u309A': 'ペ', 'ホ\u309A': 'ポ',
  'ワ\u3099': 'ヷ', 'ヰ\u3099': 'ヸ', 'ヱ\u3099': 'ヹ', 'ヲ\u3099': 'ヺ',
  'ヽ\u3099': 'ヾ',
};
export const normalizeKanaDakuten = createNormalizer(NORMALIZE_RULES_KANA_DAKUTEN);

const isValidJapanesePhrase = (codePoints: string[], start: number, length: number) =>
  // Skip splittings that cause sound marks to occur in the first position of a phrase
  !isJapaneseSoundMark(codePoints[start]!) && (start + length === codePoints.length || !isJapaneseSoundMark(codePoints[start + length]!));
export const createKanaTranscriptionEnumerator = (kuromoji: KuromojiTokenizer) => createTranscriptionEnumerator(
  isValidJapanesePhrase,
  phrase => getAllKanaReadings(kuromoji, stripJapaneseSoundMarks(normalizeKanaDakuten(phrase))),
);
export const createRomajiTranscriptionEnumerator = (kuromoji: KuromojiTokenizer) => createTranscriptionEnumerator(
  isValidJapanesePhrase,
  phrase => getAllKanaReadings(kuromoji, stripJapaneseSoundMarks(normalizeKanaDakuten(phrase))).map(kana => normalizeRomaji(toRomajiStrictly(kana))),
);
