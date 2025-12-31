// @ts-expect-error No declaration file
import hkVariants from 'opencc-js/dict/HKVariants';
// @ts-expect-error No declaration file
import hkVariantsRev from 'opencc-js/dict/HKVariantsRev';
// @ts-expect-error No declaration file
import jpVariants from 'opencc-js/dict/JPVariants';
// @ts-expect-error No declaration file
import jpVariantsRev from 'opencc-js/dict/JPVariantsRev';
// @ts-expect-error No declaration file
import stCharacters from 'opencc-js/dict/STCharacters';
// @ts-expect-error No declaration file
import tsCharacters from 'opencc-js/dict/TSCharacters';
// @ts-expect-error No declaration file
import twVariants from 'opencc-js/dict/TWVariants';
// @ts-expect-error No declaration file
import twVariantsRev from 'opencc-js/dict/TWVariantsRev';
import { polyphonic } from 'pinyin-pro';

export const unionFindSet = <T>() => {
  const parent = new Map<T, T>();
  const rank = new Map<T, number>();
  const find = (x: T): T => {
    const p = parent.get(x);
    if (p == null) {
      parent.set(x, x);
      return x;
    } else if (p === x) return x;
    else {
      const root = find(p);
      parent.set(x, root);
      return root;
    }
  };
  const union = (x: T, y: T) => {
    x = find(x);
    y = find(y);
    if (x === y) return;
    const rankX = rank.get(x) ?? 0, rankY = rank.get(y) ?? 0;
    if (rankX < rankY) parent.set(x, y);
    else if (rankX > rankY) parent.set(y, x);
    else {
      parent.set(y, x);
      rank.set(x, rankX + 1);
    }
  };
  const keys = () => parent.keys();
  return { find, union, keys };
};

const exchangeMap = (() => {
  const ufs = unionFindSet<string>();
  for (const dict of [hkVariants, hkVariantsRev, jpVariants, jpVariantsRev, stCharacters, tsCharacters, twVariants, twVariantsRev] as string[]) {
    for (const [from, to] of dict.split('|').map(pair => pair.split(' '))) {
      if (!from || !to || [...from].length !== 1 || [...to].length !== 1) continue;
      ufs.union(from, to);
    }
  }
  const map = new Map<string, string[]>();
  for (const key of ufs.keys()) {
    const root = ufs.find(key);
    let list = map.get(root);
    if (!list) map.set(root, list = []);
    if (key !== root) map.set(key, list);
    list.push(key);
  }
  for (const list of map.values()) list.sort();
  return map;
})();

export const isHanCharacter = (phrase: string) => /^[\p{Script=Han}]+$/u.test(phrase);

export const getHanVariants = (character: string) => exchangeMap.get(character) ?? (isHanCharacter(character) ? [character] : []);

const PINYIN_INITIALS: string[] = ['b', 'p', 'm', 'f', 'd', 't', 'n', 'l', 'g', 'k', 'h', 'j', 'q', 'x', 'zh', 'ch', 'sh', 'r', 'z', 'c', 's', 'y', 'w'];
const PINYIN_FINALS_FUZZY_MAP: Record<string, string> = { 'ang': 'an', 'eng': 'en', 'ing': 'in' };
export const getPinyinCandidates = (character: string) => {
  const pinyins = polyphonic(character, { type: 'array', toneType: 'none', removeNonZh: true })[0] ?? [];
  return Array.from(new Set(pinyins.filter(fullPinyin => fullPinyin).flatMap(fullPinyin => {
    const initial = PINYIN_INITIALS.find(initial => fullPinyin.startsWith(initial));
    const initialAlphabet = initial?.[0] ?? fullPinyin[0]!;
    const fuzzySuffix = fullPinyin.slice(-3);
    const fuzzyPinyin = fuzzySuffix in PINYIN_FINALS_FUZZY_MAP ? fullPinyin.slice(0, -3) + PINYIN_FINALS_FUZZY_MAP[fuzzySuffix] : undefined;
    return [fullPinyin, initial, initialAlphabet, fuzzyPinyin].filter((s): s is string => !!s);
  })));
};
