import { deserializeTrie } from './trie';
import type { TrieNode } from '../common';
import type { CompressedInvertedIndex, OffsetSpan, TokenDefinition } from '../common/types';

export interface TokenDocumentReference {
  documentId: number;
  offsets: OffsetSpan[];
}

interface TokenDefinitionExtended extends TokenDefinition {
  references: TokenDocumentReference[];
};

const mergeMap = <K, V>(...maps: Map<K, V>[]) => {
  const result = new Map<K, V>();
  for (const map of maps) for (const [key, value] of map.entries()) result.set(key, value);
  return result;
};

export interface LoadedInvertedIndex {
  documents: string[];
  documentCodePoints: string[][];
  tokenDefinitions: TokenDefinitionExtended[];
  tries: {
    romaji: TrieNode;
    kana: TrieNode;
    other: TrieNode;
  };
}

export const loadInvertedIndex = (compressed: CompressedInvertedIndex, documents?: string[]): LoadedInvertedIndex => {
  documents ??= compressed.documents;
  if (!documents) throw new Error('Loading an inverted index without documents bundled requires documents to be provided explicitly.');
  const documentCodePoints = documents.map(document => [...document]);

  const romajiTrie = deserializeTrie(compressed.tries.romaji);
  const kanaTrie = deserializeTrie(compressed.tries.kana);
  const otherTrie = deserializeTrie(compressed.tries.other);

  const tokenCodePoints = mergeMap(romajiTrie.tokenCodePoints, kanaTrie.tokenCodePoints, otherTrie.tokenCodePoints);
  const tokenDefinitions = compressed.tokenTypes.map<TokenDefinitionExtended>((type, index) => ({
    id: index, type, text: tokenCodePoints.get(index)!.join(''),
    codePointLength: tokenCodePoints.get(index)!.length,
    references: compressed.tokenReferences[index]!.map<TokenDocumentReference>(([documentId, ...offsets]) => ({
      documentId: documentId!,
      offsets: Array.from({ length: offsets.length / 2 }, (_, i) => ({ start: offsets[i * 2]!, end: offsets[i * 2 + 1]! })),
    })),
  }));

  return {
    documents,
    documentCodePoints,
    tokenDefinitions,
    tries: {
      romaji: romajiTrie.root,
      kana: kanaTrie.root,
      other: otherTrie.root,
    },
  };
};
