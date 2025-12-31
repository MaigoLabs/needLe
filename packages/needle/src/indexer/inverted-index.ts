import { NORMALIZE_RULES_KANA_DAKUTEN, NORMALIZE_RULES_ROMAJI } from './japanese';
import { createTokenizer, type TokenizerOptions } from './tokenizer';
import { buildTrie, graftTriePaths, serializeTrie } from './trie';
import type { CompressedInvertedIndex, TokenDefinition } from '../common/types';
import { TokenType } from '../common/types';

const buildTypedTrie = (tokens: TokenDefinition[], typePredicate: (tokenType: TokenType) => boolean) =>
  buildTrie(tokens.filter(token => typePredicate(token.type)).map(token => [token.id, token.text]));

export const buildInvertedIndex = (documents: string[], tokenizerOptions: TokenizerOptions) => {
  const tokenizer = createTokenizer(tokenizerOptions);
  const documentTokens = documents.map(document => tokenizer.tokenize(document));

  const tokenDefinitions = [...tokenizer.tokens.values()];
  const romajiRoot = buildTypedTrie(tokenDefinitions, type => type === TokenType.Romaji);
  const kanaRoot = buildTypedTrie(tokenDefinitions, type => type === TokenType.Kana);
  const otherRoot = buildTypedTrie(tokenDefinitions, type => type !== TokenType.Romaji && type !== TokenType.Kana);
  graftTriePaths(romajiRoot, NORMALIZE_RULES_ROMAJI);
  graftTriePaths(kanaRoot, NORMALIZE_RULES_KANA_DAKUTEN);

  const invertedIndex: CompressedInvertedIndex = {
    documents,
    tokenTypes: tokenDefinitions.map(token => token.type),
    tokenReferences: Array.from({ length: tokenDefinitions.length }, () => []),
    tries: {
      romaji: serializeTrie(romajiRoot),
      kana: serializeTrie(kanaRoot),
      other: serializeTrie(otherRoot),
    },
  };
  for (const [documentId, tokens] of documentTokens.entries()) {
    const tokenOccurrences = new Map<number, number[]>();
    for (const token of tokens) {
      let occurrences = tokenOccurrences.get(token.id);
      if (!occurrences) {
        occurrences = [];
        tokenOccurrences.set(token.id, occurrences);
      }
      occurrences.push(token.start, token.end);
    }
    for (const [tokenId, occurrences] of tokenOccurrences) {
      invertedIndex.tokenReferences[tokenId]!.push([documentId, ...occurrences]);
    }
  }
  return invertedIndex;
};
