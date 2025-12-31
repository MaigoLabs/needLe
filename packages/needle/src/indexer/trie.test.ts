import { traverseTrie } from '../common';
import { buildTrie, graftTriePaths } from './trie';

describe('graftTriePaths', () => {
  it('should graft paths according to normalization rules', () => {
    // Build a trie with tokens containing normalized forms
    const trie = buildTrie([
      [0, 'sya'], // normalized form of "sha"
      [1, 'tu'],  // normalized form of "tsu"
    ]);

    // Graft paths so that "sha" -> "sya" and "tsu" -> "tu"
    graftTriePaths(trie, {
      sha: 'sya',
      tsu: 'tu',
    });

    // Now we should be able to traverse using both the original and grafted paths
    const syaNode = traverseTrie(trie, 'sya');
    const shaNode = traverseTrie(trie, 'sha');
    expect(syaNode).toBeDefined();
    expect(shaNode).toBeDefined();
    expect(syaNode).toBe(shaNode); // Both paths should lead to the same node

    const tuNode = traverseTrie(trie, 'tu');
    const tsuNode = traverseTrie(trie, 'tsu');
    expect(tuNode).toBeDefined();
    expect(tsuNode).toBeDefined();
    expect(tuNode).toBe(tsuNode);
  });

  it('should handle chained graft rules', () => {
    const trie = buildTrie([
      [0, 'o'], // normalized vowel
    ]);

    // Chain: "ou" -> "o", "oo" -> "o"
    graftTriePaths(trie, {
      ou: 'o',
      oo: 'o',
    });

    const oNode = traverseTrie(trie, 'o');
    const ouNode = traverseTrie(trie, 'ou');
    const ooNode = traverseTrie(trie, 'oo');

    expect(oNode).toBeDefined();
    expect(ouNode).toBe(oNode);
    expect(ooNode).toBe(oNode);
  });
});
