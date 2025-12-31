using MaigoLabs.NeedLe.Common;
using MaigoLabs.NeedLe.Common.Extensions;
using MaigoLabs.NeedLe.Common.Types;

namespace MaigoLabs.NeedLe.Searcher;

public class SearchResultToken
{
    public required TokenDefinition Definition { get; set; }
    public required OffsetSpan DocumentOffset { get; set; }
    public required OffsetSpan InputOffset { get; set; }
    public required bool IsTokenPrefixMatching { get; set; }
}

public class SearchResult
{
    public required int DocumentId { get; set; }
    public required string DocumentText { get; set; }
    public required int[] DocumentCodePoints { get; set; }
    public required SearchResultToken[] Tokens { get; set; }
    public required int PrefixMatchCount { get; set; }
    public required int RangeCount { get; set; }
    public required double MatchRatio { get; set; }
    public required int MatchRatioLevel { get; set; }
}

public static class InvertedIndexSearcher
{
    public abstract class ComparableStateBase<T> : IComparable<T>
        where T : ComparableStateBase<T>
    {
        protected abstract int GetRangeCount();
        protected abstract int GetPrefixMatchCount();
        protected abstract OffsetSpan GetFirstTokenDocumentOffset();
        protected abstract OffsetSpan GetLastTokenDocumentOffset();
        protected virtual SearchResultToken? GetLastToken() => null; // Not on intermediate results
        protected virtual int? GetMatchRatioLevel() => null; // Not on intermediate/candidate results
        protected abstract double GetMatchRatio();
        protected virtual int FallbackCompareTo(T other) => 0; // Called when all other comparisons are equal

        public int CompareTo(T other)
        {
            // Prefer matches that not relying on end-of-input loose matching (full match over prefix match)
            SearchResultToken? aLastToken = GetLastToken(), bLastToken = other.GetLastToken();
            if (aLastToken != null && bLastToken != null)
            {
                var aDidPrefixMatchByTokenType = aLastToken.IsTokenPrefixMatching && tokenTypePrefixMatchingPolicy[aLastToken.Definition.Type] == TokenTypePrefixMatchingPolicy.AllowOnlyAtInputEnd;
                var bDidPrefixMatchByTokenType = bLastToken.IsTokenPrefixMatching && tokenTypePrefixMatchingPolicy[bLastToken.Definition.Type] == TokenTypePrefixMatchingPolicy.AllowOnlyAtInputEnd;
                if (aDidPrefixMatchByTokenType != bDidPrefixMatchByTokenType) return aDidPrefixMatchByTokenType ? 1 : -1;
            }

            // Prefer results that matched fewer discontinuous ranges over more
            int aRangeCount = GetRangeCount(), bRangeCount = other.GetRangeCount();
            if (aRangeCount != bRangeCount) return aRangeCount - bRangeCount;

            // Prefer results that matches first token in document earlier over later
            OffsetSpan aFirstTokenDocumentOffset = GetFirstTokenDocumentOffset(), bFirstTokenDocumentOffset = other.GetFirstTokenDocumentOffset();
            if (aFirstTokenDocumentOffset.Start != bFirstTokenDocumentOffset.Start) return aFirstTokenDocumentOffset.Start - bFirstTokenDocumentOffset.Start;

            // Prefer results that has higher match ratio (but don't distinguish similar ratios, so we introduced `matchRatioLevel`)
            int? aMatchRatioLevel = GetMatchRatioLevel(), bMatchRatioLevel = other.GetMatchRatioLevel();
            if (aMatchRatioLevel != null && bMatchRatioLevel != null)
            {
                if (aMatchRatioLevel.Value != bMatchRatioLevel.Value) return bMatchRatioLevel.Value - aMatchRatioLevel.Value;
            }

            // Prefer results that last token occurred earlier (if same, ended earlier) in the document over later
            OffsetSpan aLastTokenDocumentOffset = GetLastTokenDocumentOffset(), bLastTokenDocumentOffset = other.GetLastTokenDocumentOffset();
            if (aLastTokenDocumentOffset.Start != bLastTokenDocumentOffset.Start) return aLastTokenDocumentOffset.Start - bLastTokenDocumentOffset.Start;
            if (aLastTokenDocumentOffset.End != bLastTokenDocumentOffset.End) return aLastTokenDocumentOffset.End - bLastTokenDocumentOffset.End;

            // Prefer results that has higher match ratio (precisely)
            double aMatchRatio = GetMatchRatio(), bMatchRatio = other.GetMatchRatio();
            if (aMatchRatio != bMatchRatio) return bMatchRatio < aMatchRatio ? -1 : bMatchRatio > aMatchRatio ? 1 : 0;

            return FallbackCompareTo(other);
        }
    }

    public class IntermediateResult : ComparableStateBase<IntermediateResult>
    {
        public required IntermediateResult? PreviousState { get; init; }
        public required OffsetSpan FirstTokenDocumentOffset { get; init; }
        public required int RangeCount { get; init; }
        public required int TokenCount { get; init; }
        public required int PrefixMatchCount { get; init; }
        public required double MatchedTokenLength { get; init; }
        public required int TokenId { get; init; }
        public required OffsetSpan DocumentOffset { get; init; }
        public required OffsetSpan InputOffset { get; init; }
        public required bool IsTokenPrefixMatching { get; init; }

        protected override int GetRangeCount() => RangeCount;
        protected override int GetPrefixMatchCount() => PrefixMatchCount;
        protected override OffsetSpan GetFirstTokenDocumentOffset() => FirstTokenDocumentOffset;
        protected override OffsetSpan GetLastTokenDocumentOffset() => DocumentOffset;
        protected override double GetMatchRatio() => MatchedTokenLength; // No need to divide document length since intermediate results are for same document
    }

    public class CandidateResult : ComparableStateBase<CandidateResult>
    {
        public required SearchResultToken[] Tokens { get; init; }
        public required int PrefixMatchCount { get; init; }
        public required double MatchedTokenLength { get; init; }
        public required int RangeCount { get; init; }

        protected override int GetRangeCount() => RangeCount;
        protected override int GetPrefixMatchCount() => PrefixMatchCount;
        protected override OffsetSpan GetFirstTokenDocumentOffset() => Tokens[0].DocumentOffset;
        protected override OffsetSpan GetLastTokenDocumentOffset() => Tokens[^1].DocumentOffset;
        protected override SearchResultToken? GetLastToken() => Tokens[^1];
        protected override double GetMatchRatio() => MatchedTokenLength; // No need to divide document length since intermediate results are for same document
    }

    public class FinalResult : ComparableStateBase<FinalResult>
    {
        public required SearchResult Result { get; init; }

        protected override int GetRangeCount() => Result.RangeCount;
        protected override int GetPrefixMatchCount() => Result.PrefixMatchCount;
        protected override OffsetSpan GetFirstTokenDocumentOffset() => Result.Tokens[0].DocumentOffset;
        protected override OffsetSpan GetLastTokenDocumentOffset() => Result.Tokens[^1].DocumentOffset;
        protected override SearchResultToken? GetLastToken() => Result.Tokens[^1];
        protected override double GetMatchRatio() => Result.MatchRatio;
        protected override int? GetMatchRatioLevel() => Result.MatchRatioLevel;
        protected override int FallbackCompareTo(FinalResult other) => string.Compare(Result.DocumentText, other.Result.DocumentText, StringComparison.InvariantCulture);
    }

    private static bool IsIgnorableCodePoint(int codePoint) => CommonUtils.IsWhitespace(codePoint) || codePoint == 0x3099 || codePoint == 0x309A;

    public enum TokenTypePrefixMatchingPolicy {
        AlwaysAllow,
        NeverAllow,
        AllowOnlyAtInputEnd,
    }

    private static Dictionary<TokenType, TokenTypePrefixMatchingPolicy> tokenTypePrefixMatchingPolicy = new()
    {
        [TokenType.Romaji] = TokenTypePrefixMatchingPolicy.NeverAllow,
        [TokenType.Kana] = TokenTypePrefixMatchingPolicy.AlwaysAllow,
        // These token types are in an "other" Trie
        [TokenType.Han] = TokenTypePrefixMatchingPolicy.AllowOnlyAtInputEnd, // No effect because always 1 code point
        [TokenType.Pinyin] = TokenTypePrefixMatchingPolicy.AllowOnlyAtInputEnd,
        [TokenType.Raw] = TokenTypePrefixMatchingPolicy.AllowOnlyAtInputEnd, // No effect because always 1 code point
    };

    private static bool ShouldAllowPrefixMatching(TokenType tokenType, bool isAtInputEnd) =>
        tokenTypePrefixMatchingPolicy[tokenType] == TokenTypePrefixMatchingPolicy.AlwaysAllow ||
        (tokenTypePrefixMatchingPolicy[tokenType] != TokenTypePrefixMatchingPolicy.NeverAllow && isAtInputEnd);

    private static bool HasNonEmptyCharacters(int[] documentCodePoints, int start, int end) =>
        start != end && !documentCodePoints.Skip(start).Take(end - start).All(CommonUtils.IsWhitespace);

    public static SearchResult[] Search(LoadedInvertedIndex invertedIndex, string text)
    {
        var documents = invertedIndex.Documents;
        var documentCodePoints = invertedIndex.DocumentCodePoints;
        var tokenDefinitions = invertedIndex.TokenDefinitions;
        var tries = invertedIndex.Tries;

        var codePoints = text.ToCodePoints().Select(CommonNormalization.NormalizeCodePoint).Select(CommonNormalization.ToKatakana).ToArray();
        // dp[i] = docId => end => IntermediateResult, starts from dp[-1] (l === 0), ends at dp[N - 1] (r === N - 1)
        var dp = Enumerable.Range(0, codePoints.Length).Select(l => new Dictionary<int, Dictionary<int, IntermediateResult>>()).ToArray();
        for (var l = 0; l < codePoints.Length; l++)
        {
            if (l != 0 && dp[l - 1].Count == 0) continue; // No documents match input from beginning to this position
            var romajiNode = tries.Romaji;
            var kanaNode = tries.Kana;
            var otherNode = tries.Other;
            for (var r = l; r < codePoints.Length && (romajiNode != null || kanaNode != null || otherNode != null); r++) // [l, r]
            {
                var codePoint = codePoints[r];
                romajiNode = romajiNode.TraverseStep(codePoint, IsIgnorableCodePoint(codePoint));
                kanaNode = kanaNode.TraverseStep(codePoint, IsIgnorableCodePoint(codePoint));
                otherNode = otherNode.TraverseStep(codePoint, IsIgnorableCodePoint(codePoint));
                var reachingInputEnd = r == codePoints.Length - 1;
                HashSet<int> matchingTokenIds =
                [
                    // Allow suffix matching of romaji/other tokens if we're at the end of the input
                    .. romajiNode.GetTokenIds(ShouldAllowPrefixMatching(TokenType.Romaji, reachingInputEnd)),
                    .. kanaNode.GetTokenIds(ShouldAllowPrefixMatching(TokenType.Kana, reachingInputEnd)),
                    .. otherNode.GetTokenIds(reachingInputEnd),
                ];
                foreach (var tokenId in matchingTokenIds) foreach (var reference in tokenDefinitions[tokenId].References)
                {
                    var isTokenPrefixMatching = !romajiNode.IsTokenExactMatch(tokenId) && !kanaNode.IsTokenExactMatch(tokenId) && !otherNode.IsTokenExactMatch(tokenId);
                    var previousMatchesOfDocument = l != 0 && dp[l - 1].TryGetValue(reference.DocumentId, out var previousMatches) ? previousMatches : null;
                    if (l != 0 && previousMatchesOfDocument == null) continue;
                    foreach (var documentOffset in reference.Offsets)
                    {
                        int currentStart = documentOffset.Start, currentEnd = documentOffset.End;
                        if (l == 0) ContributeNextMatchingState(null);
                        else foreach (var (previousEnd, previousMatch) in previousMatchesOfDocument!) if (currentStart >= previousEnd) ContributeNextMatchingState(previousMatch);
                        void ContributeNextMatchingState(IntermediateResult? previousState)
                        {
                            var nextMatchingMap = dp[r];
                            if (!nextMatchingMap.TryGetValue(reference.DocumentId, out var nextMatches)) nextMatches = nextMatchingMap[reference.DocumentId] = [];
                            var oldResult = nextMatches.TryGetValue(currentEnd, out var result) ? result : null;
                            var inputOffset = new OffsetSpan { Start = l, End = r + 1 };
                            var newResult = new IntermediateResult
                            {
                                PreviousState = previousState,
                                FirstTokenDocumentOffset = previousState?.FirstTokenDocumentOffset ?? documentOffset,
                                RangeCount = previousState == null ? 1 :
                                    previousState.RangeCount + (HasNonEmptyCharacters(documentCodePoints[reference.DocumentId], previousState.DocumentOffset.End, currentStart) ? 1 : 0),
                                TokenCount = (previousState?.TokenCount ?? 0) + 1,
                                PrefixMatchCount = (previousState?.PrefixMatchCount ?? 0) + (isTokenPrefixMatching ? 1 : 0),
                                MatchedTokenLength = (previousState?.MatchedTokenLength ?? 0) + documentOffset.Length *
                                    Math.Min(isTokenPrefixMatching ? (double)inputOffset.Length / tokenDefinitions[tokenId].CodePointLength : double.PositiveInfinity, 1),
                                TokenId = tokenId,
                                DocumentOffset = documentOffset,
                                InputOffset = inputOffset,
                                IsTokenPrefixMatching = isTokenPrefixMatching,
                            };
                            nextMatches[currentEnd] = oldResult == null || newResult.CompareTo(oldResult) < 0 ? newResult : oldResult;
                        }
                    }
                }
            }
        }

        // Build search results and sort documents
        return dp[codePoints.Length - 1].Select(entry =>
        {
            var (documentId, matches) = entry;
            var sortedMatches = matches.Values.Select(match =>
            {
                var tokens = new List<SearchResultToken>();
                // Build token list from backtracking
                var state = match;
                while (state != null)
                {
                    tokens.Add(new SearchResultToken
                    {
                        Definition = tokenDefinitions[state.TokenId],
                        DocumentOffset = state.DocumentOffset, InputOffset = state.InputOffset,
                        IsTokenPrefixMatching = state.IsTokenPrefixMatching,
                    });
                    state = state.PreviousState;
                }
                tokens.Reverse();
                return new CandidateResult
                {
                    Tokens = tokens.ToArray(),
                    PrefixMatchCount = match.PrefixMatchCount,
                    MatchedTokenLength = match.MatchedTokenLength,
                    RangeCount = match.RangeCount,
                };
            }).OrderBy(match => match);
            var bestMatch = sortedMatches.First();
            var documentText = documents[documentId];
            var matchRatio = bestMatch.MatchedTokenLength / documentCodePoints[documentId].Length;
            var matchRatioLevel = (int)Math.Round(matchRatio * 5);
            return new FinalResult
            {
                Result = new SearchResult
                {
                    DocumentId = documentId,
                    DocumentText = documentText,
                    DocumentCodePoints = documentCodePoints[documentId],
                    Tokens = bestMatch.Tokens,
                    PrefixMatchCount = bestMatch.PrefixMatchCount,
                    RangeCount = bestMatch.RangeCount,
                    MatchRatio = matchRatio,
                    MatchRatioLevel = matchRatioLevel,
                }
            };
        }).OrderBy(result => result).Select(result => result.Result).ToArray();
    }
}
