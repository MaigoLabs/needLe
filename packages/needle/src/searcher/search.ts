import { highlightSearchResult } from './highlight';
import { getTrieNodeTokenIds } from './trie';
import type { TrieNode } from '../common';
import { traverseTrieStep } from '../common';
import type { LoadedInvertedIndex } from './inverted-index';
import { normalizeByCodePoint, toKatakana } from '../common/normalize';
import { type OffsetSpan, type TokenDefinition, TokenType } from '../common/types';
import { getSpanLength } from '../common/utils';

const IGNORABLE_CODE_POINTS = /[\s\u3099\u309A]/u;

enum TokenTypePrefixMatchingPolicy {
  AlwaysAllow,
  NeverAllow,
  AllowOnlyAtInputEnd,
}
const tokenTypePrefixMatchingPolicy: Record<TokenType, TokenTypePrefixMatchingPolicy> = {
  [TokenType.Romaji]: TokenTypePrefixMatchingPolicy.NeverAllow,
  [TokenType.Kana]: TokenTypePrefixMatchingPolicy.AlwaysAllow,
  // These token types are in an "other" Trie
  [TokenType.Han]: TokenTypePrefixMatchingPolicy.AllowOnlyAtInputEnd, // No effect because always 1 code point
  [TokenType.Pinyin]: TokenTypePrefixMatchingPolicy.AllowOnlyAtInputEnd,
  [TokenType.Raw]: TokenTypePrefixMatchingPolicy.AllowOnlyAtInputEnd, // No effect because always 1 code point
};
const shouldAllowPrefixMatching = (tokenType: TokenType, isAtInputEnd: boolean) =>
  tokenTypePrefixMatchingPolicy[tokenType] === TokenTypePrefixMatchingPolicy.AlwaysAllow ||
  (tokenTypePrefixMatchingPolicy[tokenType] !== TokenTypePrefixMatchingPolicy.NeverAllow && isAtInputEnd);

export interface SearchResultToken {
  definition: TokenDefinition;
  documentOffset: OffsetSpan;
  inputOffset: OffsetSpan;
  isTokenPrefixMatching: boolean;
}

interface ComparableStateTraits<T> {
  getRangeCount: (state: T) => number;
  getPrefixMatchCount: (state: T) => number;
  getFirstTokenDocumentOffset: (state: T) => OffsetSpan;
  getLastTokenDocumentOffset: (state: T) => OffsetSpan;
  getLastToken?: (state: T) => SearchResultToken; // Not on intermediate results
  getMatchRatioLevel?: (state: T) => number; // Not on intermediate/candidate results
  getMatchRatio: (state: T) => number;
  // Called when all other comparisons are equal
  nextComparer?: (a: T, b: T) => number; // Not on intermediate/candidate results
}

const getComparerForTraits = <T>(traits: ComparableStateTraits<T>) => (a: T, b: T) => {
  // Prefer matches that not relying on end-of-input loose matching (full match over prefix match)
  if (traits.getLastToken) {
    const aLastToken = traits.getLastToken(a), bLastToken = traits.getLastToken(b);
    const aDidPrefixMatchByTokenType = aLastToken.isTokenPrefixMatching && tokenTypePrefixMatchingPolicy[aLastToken.definition.type] === TokenTypePrefixMatchingPolicy.AllowOnlyAtInputEnd;
    const bDidPrefixMatchByTokenType = bLastToken.isTokenPrefixMatching && tokenTypePrefixMatchingPolicy[bLastToken.definition.type] === TokenTypePrefixMatchingPolicy.AllowOnlyAtInputEnd;
    if (aDidPrefixMatchByTokenType !== bDidPrefixMatchByTokenType) return aDidPrefixMatchByTokenType ? 1 : -1;
  }

  // Prefer results that matched fewer discontinuous ranges over more
  const aRangeCount = traits.getRangeCount(a), bRangeCount = traits.getRangeCount(b);
  if (aRangeCount !== bRangeCount) return aRangeCount - bRangeCount;

  // Prefer results that matches first token in document earlier over later
  const aFirstTokenDocumentOffset = traits.getFirstTokenDocumentOffset(a), bFirstTokenDocumentOffset = traits.getFirstTokenDocumentOffset(b);
  if (aFirstTokenDocumentOffset.start !== bFirstTokenDocumentOffset.start) return aFirstTokenDocumentOffset.start - bFirstTokenDocumentOffset.start;

  // Prefer results that has higher match ratio (but don't distinguish similar ratios, so we introduced `matchRatioLevel`)
  if (traits.getMatchRatioLevel) {
    const aMatchRatioLevel = traits.getMatchRatioLevel(a), bMatchRatioLevel = traits.getMatchRatioLevel(b);
    if (aMatchRatioLevel !== bMatchRatioLevel) return bMatchRatioLevel - aMatchRatioLevel;
  }

  // Prefer results that last token occurred earlier (if same, ended earlier) in the document over later
  const aLastTokenDocumentOffset = traits.getLastTokenDocumentOffset(a), bLastTokenDocumentOffset = traits.getLastTokenDocumentOffset(b);
  if (aLastTokenDocumentOffset.start !== bLastTokenDocumentOffset.start) return aLastTokenDocumentOffset.start - bLastTokenDocumentOffset.start;
  if (aLastTokenDocumentOffset.end !== bLastTokenDocumentOffset.end) return aLastTokenDocumentOffset.end - bLastTokenDocumentOffset.end;

  // Prefer results that has higher match ratio (precisely)
  const aMatchRatio = traits.getMatchRatio(a), bMatchRatio = traits.getMatchRatio(b);
  if (aMatchRatio !== bMatchRatio) return bMatchRatio - aMatchRatio;

  return traits.nextComparer?.(a, b) ?? 0;
};

interface IntermediateResult {
  previousState?: IntermediateResult;
  firstTokenDocumentOffset: OffsetSpan;
  rangeCount: number;
  tokenCount: number;
  prefixMatchCount: number;
  matchedTokenLength: number;
  tokenId: number;
  documentOffset: OffsetSpan;
  inputOffset: OffsetSpan;
  isTokenPrefixMatching: boolean;
}
const compareIntermediateResult = getComparerForTraits<IntermediateResult>({
  getRangeCount: state => state.rangeCount,
  getPrefixMatchCount: state => state.prefixMatchCount,
  getFirstTokenDocumentOffset: state => state.firstTokenDocumentOffset,
  getLastTokenDocumentOffset: state => state.documentOffset,
  getMatchRatio: state => state.matchedTokenLength, // No need to divide document length since intermediate results are for same document
});

interface CandidateResult {
  tokens: SearchResultToken[];
  prefixMatchCount: number;
  matchedTokenLength: number;
  rangeCount: number;
}
const compareCandidateResult = getComparerForTraits<CandidateResult>({
  getRangeCount: state => state.rangeCount,
  getPrefixMatchCount: state => state.prefixMatchCount,
  getFirstTokenDocumentOffset: state => state.tokens[0]!.documentOffset,
  getLastTokenDocumentOffset: state => state.tokens[state.tokens.length - 1]!.documentOffset,
  getLastToken: state => state.tokens[state.tokens.length - 1]!,
  getMatchRatio: state => state.matchedTokenLength, // No need to divide document length since candidate results are for same document
});

export interface SearchResult {
  documentId: number;
  documentText: string;
  documentCodePoints: string[];
  tokens: SearchResultToken[];
  prefixMatchCount: number;
  rangeCount: number;
  matchRatio: number;
  matchRatioLevel: number;
}
const compareFinalResult = getComparerForTraits<SearchResult>({
  getRangeCount: state => state.rangeCount,
  getPrefixMatchCount: state => state.prefixMatchCount,
  getFirstTokenDocumentOffset: state => state.tokens[0]!.documentOffset,
  getLastTokenDocumentOffset: state => state.tokens[state.tokens.length - 1]!.documentOffset,
  getLastToken: state => state.tokens[state.tokens.length - 1]!,
  getMatchRatio: state => state.matchRatio,
  getMatchRatioLevel: state => Math.round(state.matchRatio * 5),
});

const hasNonEmptyCharacters = (documentCodePoints: string[], start: number, end: number) => start !== end && !documentCodePoints.slice(start, end).every(char => /\s/.test(char));

export const searchInvertedIndex = (
  invertedIndex: LoadedInvertedIndex,
  text: string,
  options?: {
    /**
     * Called when all other comparisons are equal.
     */
    nextComparer?: (documentIdA: number, documentIdB: number) => number;
    /**
     * If return falsy value for a document, it will be excluded from the final results.
     */
    filterDocument?: (documentId: number) => unknown;
  },
): SearchResult[] => {
  if (!text.trim()) return [];

  const { documents, documentCodePoints, tokenDefinitions, tries } = invertedIndex;

  const codePoints = [...toKatakana(normalizeByCodePoint(text))];
  // dp[i] = docId => end => IntermediateResult, starts from dp[-1] (l === 0), ends at dp[N - 1] (r === N - 1)
  const dp = Array.from({ length: codePoints.length }, () => new Map<number, Record<number, IntermediateResult>>());
  for (let l = 0; l < codePoints.length; l++) {
    if (l !== 0 && dp[l - 1]!.size === 0) continue; // No documents match input from beginning to this position
    let romajiNode: TrieNode | undefined = tries.romaji;
    let kanaNode: TrieNode | undefined = tries.kana;
    let otherNode: TrieNode | undefined = tries.other;
    for (let r = l; r < codePoints.length && (romajiNode || kanaNode || otherNode); r++) { // [l, r]
      const codePoint = codePoints[r]!;
      const nextRomajiNode = traverseTrieStep(romajiNode, codePoint, IGNORABLE_CODE_POINTS);
      const nextKanaNode = traverseTrieStep(kanaNode, codePoint, IGNORABLE_CODE_POINTS);
      const nextOtherNode = traverseTrieStep(otherNode, codePoint, IGNORABLE_CODE_POINTS);
      if (nextRomajiNode === romajiNode && nextKanaNode === kanaNode && nextOtherNode === otherNode) continue; // This code point is fully ignored on current state
      romajiNode = nextRomajiNode;
      kanaNode = nextKanaNode;
      otherNode = nextOtherNode;
      const reachingInputEnd = r === codePoints.length - 1;
      const matchingTokenIds = new Set([
        // Allow suffix matching of romaji/other tokens if we're at the end of the input
        ...getTrieNodeTokenIds(romajiNode, shouldAllowPrefixMatching(TokenType.Romaji, reachingInputEnd)),
        ...getTrieNodeTokenIds(kanaNode, shouldAllowPrefixMatching(TokenType.Kana, reachingInputEnd)),
        ...getTrieNodeTokenIds(otherNode, reachingInputEnd),
      ]);
      for (const tokenId of matchingTokenIds) for (const { documentId, offsets } of tokenDefinitions[tokenId]!.references) {
        if (options?.filterDocument && !options.filterDocument(documentId)) continue;
        const isTokenPrefixMatching = !romajiNode?.tokenIds.includes(tokenId) && !kanaNode?.tokenIds.includes(tokenId) && !otherNode?.tokenIds.includes(tokenId);
        const previousMatchesOfDocument = dp[l - 1]?.get(documentId);
        if (l !== 0 && !previousMatchesOfDocument) continue;
        for (const documentOffset of offsets) {
          const { start: currentStart, end: currentEnd } = documentOffset;
          const contributeNextMatchingState = (previousState: IntermediateResult | undefined) => {
            const nextMatchingMap = dp[r]!;
            let nextMatchesOfDocument = nextMatchingMap.get(documentId);
            if (!nextMatchesOfDocument) {
              nextMatchesOfDocument = Object.create(null) as Record<number, IntermediateResult>;
              nextMatchingMap.set(documentId, nextMatchesOfDocument);
            }
            const oldResult = nextMatchesOfDocument[currentEnd];
            const inputOffset = { start: l, end: r + 1 };
            const newResult: IntermediateResult = {
              previousState,
              firstTokenDocumentOffset: previousState?.firstTokenDocumentOffset ?? documentOffset,
              rangeCount: !previousState ? 1
                : (previousState.rangeCount + (hasNonEmptyCharacters(documentCodePoints[documentId]!, previousState.documentOffset.end, currentStart) ? 1 : 0)),
              tokenCount: (previousState?.tokenCount ?? 0) + 1,
              prefixMatchCount: (previousState?.prefixMatchCount ?? 0) + (isTokenPrefixMatching ? 1 : 0),
              matchedTokenLength: (previousState?.matchedTokenLength ?? 0) + getSpanLength(documentOffset) *
                Math.min(isTokenPrefixMatching ? getSpanLength(inputOffset) / tokenDefinitions[tokenId]!.codePointLength : Infinity, 1),
              tokenId,
              documentOffset,
              inputOffset,
              isTokenPrefixMatching,
            };
            nextMatchesOfDocument[currentEnd] = !oldResult || compareIntermediateResult(newResult, oldResult) < 0 ? newResult : oldResult;
          };
          if (l === 0) contributeNextMatchingState(undefined);
          else for (const previousEnd in previousMatchesOfDocument) if (currentStart >= Number(previousEnd))
            contributeNextMatchingState(previousMatchesOfDocument[previousEnd as unknown as number]!);
          // Don't `break` here because keys of `previousMatchesOfDocument` are not essentially ordered
        }
      }
    }
  }

  // Build search results and sort documents
  return [...dp[codePoints.length - 1]!.entries()].map<SearchResult>(([documentId, matches]) => {
    const sortedMatches = Object.values(matches).map<CandidateResult>(match => {
      const tokens: SearchResultToken[] = [];
      // Build token list from backtracking
      let state: IntermediateResult | undefined = match;
      while (state) {
        tokens.unshift({
          definition: tokenDefinitions[state.tokenId]!,
          documentOffset: state.documentOffset, inputOffset: state.inputOffset,
          isTokenPrefixMatching: state.isTokenPrefixMatching,
        });
        state = state.previousState;
      }
      return { tokens, prefixMatchCount: match.prefixMatchCount, matchedTokenLength: match.matchedTokenLength, rangeCount: match.rangeCount };
    }).sort(compareCandidateResult);
    const bestMatchOfDocument = sortedMatches[0]!;
    const documentText = documents[documentId]!;
    const matchRatio = bestMatchOfDocument.matchedTokenLength / documentCodePoints[documentId]!.length;
    const matchRatioLevel = Math.round(matchRatio * 5);
    return {
      documentId,
      documentText,
      documentCodePoints: documentCodePoints[documentId]!,
      tokens: bestMatchOfDocument.tokens,
      prefixMatchCount: bestMatchOfDocument.prefixMatchCount,
      rangeCount: bestMatchOfDocument.rangeCount,
      matchRatio,
      matchRatioLevel,
    };
  }).sort((a, b) => {
    const compareResult = compareFinalResult(a, b);
    if (compareResult !== 0) return compareResult;
    return options?.nextComparer
      ? options.nextComparer(a.documentId, b.documentId)
      : a.documentText === b.documentText ? 0 : a.documentText < b.documentText ? -1 : 1;
  });
};

// For debugging
export const inspectSearchResult = (resultDocument: SearchResult, htmlHighlight: boolean) => {
  const { documentText, tokens, rangeCount, matchRatio, matchRatioLevel } = resultDocument;
  const escapeHtml = (s: string) => s.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
  const escapedText = htmlHighlight ? highlightSearchResult(resultDocument).map(part =>
    typeof part === 'string' ? escapeHtml(part) : `<u><b>${escapeHtml(part.highlight)}</b></u>`).join('') : JSON.stringify(documentText);
  const description = ` (${rangeCount} ranges, ${Math.round(matchRatio * 10000) / 10000} => L${matchRatioLevel})`;
  return [
    escapedText + (htmlHighlight ? `<code>${description}</code>` : description),
    ...tokens.map(token => {
      let escapedTokenText = JSON.stringify(token.definition.text);
      let escapedDocumentText = JSON.stringify([...documentText].slice(token.documentOffset.start, token.documentOffset.end).join(''));
      if (htmlHighlight) {
        escapedTokenText = escapeHtml(escapedTokenText);
        escapedDocumentText = escapeHtml(escapedDocumentText);
      }
      const line = `    ${TokenType[token.definition.type]}: ${escapedTokenText} -> ${escapedDocumentText}${token.isTokenPrefixMatching ? ' (prefix match)' : ''}`;
      return htmlHighlight ? `<code>${line}</code>` : line;
    }),
    '',
  ].join('\n');
};
