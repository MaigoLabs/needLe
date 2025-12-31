using MaigoLabs.NeedLe.Common;
using MaigoLabs.NeedLe.Common.Extensions;
using MaigoLabs.NeedLe.Common.Types;
using MaigoLabs.NeedLe.Indexer.Japanese;
using MaigoLabs.NeedLe.Indexer.Trie;

namespace MaigoLabs.NeedLe.Indexer;

public static class InvertedIndexBuilder
{
    private static TrieNode BuildTypedTrie(IEnumerable<TokenDefinition> tokenDefinitions, Func<TokenType, bool> typePredicate) =>
        TrieBuilder.BuildTrie(tokenDefinitions
            .Where(token => typePredicate(token.Type))
            .Select(token => (token.Id, CodePoints: token.Text.ToCodePoints())));

    public static CompressedInvertedIndex BuildInvertedIndex(string[] documents, TokenizerOptions? tokenizerOptions = null)
    {
        var tokenizer = new Tokenizer(tokenizerOptions);
        var documentTokens = documents.Select(tokenizer.Tokenize).ToArray();

        var tokenDefinitions = tokenizer.Tokens.Values;
        var romajiRoot = BuildTypedTrie(tokenDefinitions, type => type == TokenType.Romaji);
        var kanaRoot = BuildTypedTrie(tokenDefinitions, type => type == TokenType.Kana);
        var otherRoot = BuildTypedTrie(tokenDefinitions, type => type != TokenType.Romaji && type != TokenType.Kana);
        TrieBuilder.GraftTriePaths(romajiRoot, JapaneseNormalization.NORMALIZE_RULES_ROMAJI_CODEPOINTS);
        TrieBuilder.GraftTriePaths(kanaRoot, JapaneseNormalization.NORMALIZE_RULES_KANA_DAKUTEN_CODEPOINTS);

        var invertedIndex = new CompressedInvertedIndex
        {
            documents = documents,
            tokenTypes = [.. tokenDefinitions.Select(token => (int)token.Type)],
            tokenReferences = [.. tokenDefinitions.Select(_ => new List<int[]>())],
            tries = new CompressedInvertedIndexTries
            {
                romaji = TrieSerializer.Serialize(romajiRoot),
                kana = TrieSerializer.Serialize(kanaRoot),
                other = TrieSerializer.Serialize(otherRoot),
            },
        };
        for (var documentId = 0; documentId < documents.Length; documentId++)
        {
            var tokens = documentTokens[documentId];
            var tokenOccurrences = new Dictionary<int, List<int>>();
            foreach (var token in tokens)
            {
                if (!tokenOccurrences.TryGetValue(token.Id, out var occurrences)) tokenOccurrences[token.Id] = occurrences = [];
                occurrences.Add(token.Start);
                occurrences.Add(token.End);
            }
            foreach (var (tokenId, occurrences) in tokenOccurrences)
            {
                invertedIndex.tokenReferences[tokenId].Add([documentId, .. occurrences]);
            }
        }
        return invertedIndex;
    }
}
