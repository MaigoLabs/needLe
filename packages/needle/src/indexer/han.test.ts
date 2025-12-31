import { getHanVariants, getPinyinCandidates, isHanCharacter, unionFindSet } from './han';

describe('unionFindSet', () => {
  it('should find self as root initially', () => {
    const ufs = unionFindSet<number>();
    expect(ufs.find(1)).toBe(1);
    expect(ufs.find(2)).toBe(2);
  });

  it('should union two elements', () => {
    const ufs = unionFindSet<number>();
    ufs.union(1, 2);
    expect(ufs.find(1)).toBe(ufs.find(2));
  });

  it('should union multiple elements transitively', () => {
    const ufs = unionFindSet<number>();
    ufs.union(1, 2);
    ufs.union(2, 3);
    ufs.union(4, 5);
    expect(ufs.find(1)).toBe(ufs.find(3));
    expect(ufs.find(1)).not.toBe(ufs.find(4));
    ufs.union(3, 4);
    expect(ufs.find(1)).toBe(ufs.find(5));
  });

  it('should iterate all keys', () => {
    const ufs = unionFindSet<string>();
    ufs.union('a', 'b');
    ufs.union('c', 'd');
    const keys = [...ufs.keys()];
    expect(keys).toContain('a');
    expect(keys).toContain('b');
    expect(keys).toContain('c');
    expect(keys).toContain('d');
  });
});

describe('isHanCharacter', () => {
  it('should return true for CJK characters', () => {
    expect(isHanCharacter('中')).toBe(true);
    expect(isHanCharacter('国')).toBe(true);
    expect(isHanCharacter('日')).toBe(true);
    expect(isHanCharacter('本')).toBe(true);
  });

  it('should return false for non-CJK characters', () => {
    expect(isHanCharacter('a')).toBe(false);
    expect(isHanCharacter('あ')).toBe(false);
    expect(isHanCharacter('ア')).toBe(false);
    expect(isHanCharacter('1')).toBe(false);
  });
});

describe('getHanVariants', () => {
  it('should return variants for simplified/traditional characters', () => {
    // 国 (simplified) and 國 (traditional) should be variants of each other
    const variants1 = getHanVariants('国');
    const variants2 = getHanVariants('國');
    expect(variants1).toContain('国');
    expect(variants1).toContain('國');
    expect(variants2).toContain('国');
    expect(variants2).toContain('國');
  });

  it('should return the character itself for characters without variants', () => {
    const variants = getHanVariants('一');
    expect(variants).toContain('一');
  });

  it('should return empty array for non-Han characters', () => {
    expect(getHanVariants('a')).toEqual([]);
    expect(getHanVariants('あ')).toEqual([]);
  });
});

describe('getPinyinCandidates', () => {
  it('should return pinyin for a Han character', () => {
    const candidates = getPinyinCandidates('中');
    expect(candidates).toContain('zhong');
    expect(candidates).toContain('zh'); // initial
    expect(candidates).toContain('z'); // first letter
  });

  it('should return multiple pinyin for polyphonic characters', () => {
    // 行 can be "xing" or "hang"
    const candidates = getPinyinCandidates('行');
    expect(candidates).toContain('xing');
    expect(candidates).toContain('hang');
  });

  it('should include fuzzy pinyin variants', () => {
    // 风 is "feng", should also have fuzzy variant "fen"
    const candidates = getPinyinCandidates('风');
    expect(candidates).toContain('feng');
    expect(candidates).toContain('fen'); // fuzzy: eng -> en
  });

  it('should return empty array for non-Han characters', () => {
    expect(getPinyinCandidates('a')).toEqual([]);
    expect(getPinyinCandidates('あ')).toEqual([]);
  });
});
