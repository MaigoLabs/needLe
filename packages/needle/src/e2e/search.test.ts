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
