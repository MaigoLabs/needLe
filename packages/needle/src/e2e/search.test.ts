import path from 'node:path';
import url from 'node:url';

import { TokenizerBuilder } from '@patdx/kuromoji';
import NodeDictionaryLoader from '@patdx/kuromoji/node';

import { buildInvertedIndex, type KuromojiTokenizer } from '../indexer';
import { highlightSearchResult, loadInvertedIndex, searchInvertedIndex } from '../searcher';

let kuromoji: KuromojiTokenizer;

beforeAll(async () => {
  const kuromojiDictPath = path.resolve(url.fileURLToPath(import.meta.resolve('@patdx/kuromoji')), '..', '..', 'dict');
  kuromoji = await new TokenizerBuilder({ loader: new NodeDictionaryLoader({ dic_path: kuromojiDictPath }) }).build();
});

describe('search', () => {
  const testDocuments = [
    'ミーティア',
    'エンドマークに希望と涙を添えて',
    '宵の鳥',
    '僕の和風本当上手',
  ];

  it('should match with mixed search query', () => {
    const compressed = buildInvertedIndex(testDocuments, { kuromoji });
    const invertedIndex = loadInvertedIndex(compressed);

    const results = searchInvertedIndex(invertedIndex, 'bokunoh风じょう');

    // Should have at least one result
    expect(results.length).toBeGreaterThan(0);

    // The first result should be "僕の和風本当上手"
    expect(results[0]!.documentText).toBe('僕の和風本当上手');
  });

  it('should highlight search result correctly', () => {
    const compressed = buildInvertedIndex(testDocuments, { kuromoji });
    const invertedIndex = loadInvertedIndex(compressed);

    const results = searchInvertedIndex(invertedIndex, 'bokunoh风じょう');
    expect(results.length).toBeGreaterThan(0);

    const highlighted = highlightSearchResult(results[0]!);

    // Should be an array of parts
    expect(Array.isArray(highlighted)).toBe(true);
    expect(highlighted.length).toBeGreaterThan(0);

    // Collect highlighted text
    const highlightedTexts = highlighted
      .filter((part): part is { highlight: string } => typeof part !== 'string')
      .map(part => part.highlight);

    expect(highlightedTexts.some(text => text.includes('僕'))).toBe(true);
    expect(highlightedTexts.some(text => text.includes('の'))).toBe(true);
    expect(highlightedTexts.some(text => text.includes('和'))).toBe(true);
    expect(highlightedTexts.some(text => text.includes('風'))).toBe(true);
    expect(highlightedTexts.some(text => text.includes('上'))).toBe(true);
  });

  it('should match romaji input to kana documents', () => {
    const compressed = buildInvertedIndex(testDocuments, { kuromoji });
    const invertedIndex = loadInvertedIndex(compressed);

    // Search for "yoi" should match "宵の鳥"
    const results = searchInvertedIndex(invertedIndex, 'yoi');
    const matchedTexts = results.map(r => r.documentText);

    expect(matchedTexts).toContain('宵の鳥');
  });
});

describe('search options', () => {
  const testDocuments = [
    'ミーティア',
    'エンドマークに希望と涙を添えて',
    '宵の鳥',
    '僕の和風本当上手',
  ];

  describe('bundleDocuments option', () => {
    it('should work when documents are not bundled and provided at load time', () => {
      const compressed = buildInvertedIndex(testDocuments, { kuromoji, bundleDocuments: false });

      // Documents should not be in the compressed index
      expect(compressed.documents).toBeUndefined();

      // Load with documents provided explicitly
      const invertedIndex = loadInvertedIndex(compressed, testDocuments);

      const results = searchInvertedIndex(invertedIndex, 'yoi');
      expect(results.map(r => r.documentText)).toContain('宵の鳥');
    });

    it('should throw when loading without documents and none provided', () => {
      const compressed = buildInvertedIndex(testDocuments, { kuromoji, bundleDocuments: false });

      expect(() => loadInvertedIndex(compressed)).toThrow();
    });
  });

  describe('filterDocument option', () => {
    it('should exclude filtered documents from results', () => {
      const compressed = buildInvertedIndex(testDocuments, { kuromoji });
      const invertedIndex = loadInvertedIndex(compressed);

      // Search without filter - should find "宵の鳥" (documentId 2)
      const resultsWithoutFilter = searchInvertedIndex(invertedIndex, 'yoi');
      expect(resultsWithoutFilter.map(r => r.documentText)).toContain('宵の鳥');

      // Search with filter excluding documentId 2
      const resultsWithFilter = searchInvertedIndex(invertedIndex, 'yoi', {
        filterDocument: id => id !== 2,
      });
      expect(resultsWithFilter.map(r => r.documentText)).not.toContain('宵の鳥');
    });
  });

  describe('nextComparer option', () => {
    it('should use custom comparer for final sorting when other criteria are equal', () => {
      // Create documents that would have similar match scores
      const similarDocs = ['テストA', 'テストB', 'テストC'];
      const compressed = buildInvertedIndex(similarDocs, { kuromoji });
      const invertedIndex = loadInvertedIndex(compressed);

      // Search with reverse order comparer
      const results = searchInvertedIndex(invertedIndex, 'テスト', {
        nextComparer: (a, b) => b - a, // Reverse by documentId
      });

      // Should be in reverse documentId order (2, 1, 0) when other criteria equal
      expect(results.map(r => r.documentId)).toEqual([2, 1, 0]);
    });
  });
});
