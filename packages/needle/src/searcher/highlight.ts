import { getSpanLength, TokenType } from '../common';
import type { SearchResult } from './search';

export type HighlightedTextPart = /* not highlighted */ string | /* highlighted */ { highlight: string };

export const highlightSearchResult = (resultDocument: SearchResult): HighlightedTextPart[] => {
  const highlightResult: HighlightedTextPart[] = [];
  let previousHighlightEnd = 0;
  for (const token of resultDocument.tokens) {
    const notHighlightedText = resultDocument.documentCodePoints.slice(previousHighlightEnd, token.documentOffset.start).join('');
    if (notHighlightedText.length > 0) highlightResult.push(notHighlightedText);
    const highlightEnd = token.isTokenPrefixMatching && (token.definition.type === TokenType.Kana)
      ? token.documentOffset.start + Math.max(
        1,
        Math.round(
          getSpanLength(token.documentOffset) *
          Math.min(1, getSpanLength(token.inputOffset) / token.definition.codePointLength),
        ),
      )
      : token.documentOffset.end;
    highlightResult.push({ highlight: resultDocument.documentCodePoints.slice(token.documentOffset.start, highlightEnd).join('') });
    previousHighlightEnd = highlightEnd;
  }
  if (previousHighlightEnd < resultDocument.documentCodePoints.length) highlightResult.push(resultDocument.documentCodePoints.slice(previousHighlightEnd).join(''));
  return highlightResult;
};
