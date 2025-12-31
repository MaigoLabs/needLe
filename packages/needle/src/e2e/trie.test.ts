import { traverseTrie } from '../common';
import { buildTrie, serializeTrie } from '../indexer/trie';
import { deserializeTrie } from '../searcher/trie';

describe('Trie building', () => {
  it('should build a Trie with multiple different tokens', () => {
    const trie = buildTrie([
      [0, 'hello'],
      [1, 'help'],
      [2, 'world'],
      [3, 'word'],
    ]);

    // Traverse to verify structure
    const helloNode = traverseTrie(trie, 'hello');
    const helpNode = traverseTrie(trie, 'help');
    const worldNode = traverseTrie(trie, 'world');
    const wordNode = traverseTrie(trie, 'word');

    expect(helloNode).toBeDefined();
    expect(helpNode).toBeDefined();
    expect(worldNode).toBeDefined();
    expect(wordNode).toBeDefined();

    // Check token IDs
    expect(helloNode!.tokenIds).toContain(0);
    expect(helpNode!.tokenIds).toContain(1);
    expect(worldNode!.tokenIds).toContain(2);
    expect(wordNode!.tokenIds).toContain(3);

    // Check that 'hel' prefix node has both tokens in subTree
    const helNode = traverseTrie(trie, 'hel');
    expect(helNode).toBeDefined();
    expect(helNode!.subTreeTokenIds).toContain(0);
    expect(helNode!.subTreeTokenIds).toContain(1);
  });

  it('should handle Japanese text tokens', () => {
    const trie = buildTrie([
      [0, 'さくら'],
      [1, 'サクラ'],
      [2, '桜'],
    ]);

    expect(traverseTrie(trie, 'さくら')?.tokenIds).toContain(0);
    expect(traverseTrie(trie, 'サクラ')?.tokenIds).toContain(1);
    expect(traverseTrie(trie, '桜')?.tokenIds).toContain(2);
  });
});

describe('Trie serialization and deserialization', () => {
  it('should serialize and deserialize a Trie correctly', () => {
    const originalTrie = buildTrie([
      [0, 'apple'],
      [1, 'app'],
      [2, 'banana'],
    ]);

    // Serialize
    const serialized = serializeTrie(originalTrie);
    expect(Array.isArray(serialized)).toBe(true);
    expect(serialized.length).toBeGreaterThan(0);

    // Deserialize
    const { root: deserializedTrie, tokenCodePoints } = deserializeTrie(serialized);

    // Verify structure is preserved
    const appleNode = traverseTrie(deserializedTrie, 'apple');
    const appNode = traverseTrie(deserializedTrie, 'app');
    const bananaNode = traverseTrie(deserializedTrie, 'banana');

    expect(appleNode).toBeDefined();
    expect(appNode).toBeDefined();
    expect(bananaNode).toBeDefined();

    expect(appleNode!.tokenIds).toContain(0);
    expect(appNode!.tokenIds).toContain(1);
    expect(bananaNode!.tokenIds).toContain(2);

    // Verify tokenCodePoints map
    expect(tokenCodePoints.get(0)?.join('')).toBe('apple');
    expect(tokenCodePoints.get(1)?.join('')).toBe('app');
    expect(tokenCodePoints.get(2)?.join('')).toBe('banana');

    // Verify subTreeTokenIds are reconstructed
    expect(appNode!.subTreeTokenIds).toContain(0);
    expect(appNode!.subTreeTokenIds).toContain(1);
  });

  it('should preserve parent references after deserialization', () => {
    const originalTrie = buildTrie([
      [0, 'test'],
    ]);

    const serialized = serializeTrie(originalTrie);
    const { root } = deserializeTrie(serialized);

    const testNode = traverseTrie(root, 'test');
    expect(testNode).toBeDefined();

    // Walk back to root via parent references
    let node = testNode;
    let depth = 0;
    while (node?.parent) {
      node = node.parent;
      depth++;
    }
    expect(depth).toBe(4); // 't' -> 'e' -> 's' -> 't' -> root
    expect(node).toBe(root);
  });
});
