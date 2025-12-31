using MaigoLabs.NeedLe.Common;
using MaigoLabs.NeedLe.Common.Extensions;
using MaigoLabs.NeedLe.Common.Types;
using MaigoLabs.NeedLe.Searcher.Trie;

namespace MaigoLabs.NeedLe.Searcher;

public class LoadedInvertedIndex
{
    public class TokenDocumentReference
    {
        public required int DocumentId { get; set; }
        public required OffsetSpan[] Offsets { get; set; }
    }

    public class TokenDefinitionExtended : TokenDefinition
    {
        public required TokenDocumentReference[] References { get; set; }
    }

    public class TypedTries
    {
        public required TrieNode Romaji { get; set; }
        public required TrieNode Kana { get; set; }
        public required TrieNode Other { get; set; }
    }

    public required string[] Documents { get; set; }
    public required int[][] DocumentCodePoints { get; set; }
    public required TokenDefinitionExtended[] TokenDefinitions { get; set; }
    public required TypedTries Tries { get; set; }
}

public class InvertedIndexLoader
{
    public static LoadedInvertedIndex Load(CompressedInvertedIndex compressed)
    {
        var documents = compressed.documents;
        var documentCodePoints = documents.Select(document => document.ToCodePoints().ToArray()).ToArray();

        var romajiTrie = TrieDeserializer.Deserialize(compressed.tries.romaji);
        var kanaTrie = TrieDeserializer.Deserialize(compressed.tries.kana);
        var otherTrie = TrieDeserializer.Deserialize(compressed.tries.other);

        var tokenCodePoints = romajiTrie.TokenCodePoints.Concat(kanaTrie.TokenCodePoints).Concat(otherTrie.TokenCodePoints)
            .ToDictionary(entry => entry.Key, entry => entry.Value);
        var tokenDefinitions = compressed.tokenTypes.Select((type, index) => new LoadedInvertedIndex.TokenDefinitionExtended
        {
            Id = index, Type = (TokenType)type, Text = tokenCodePoints[index].ToUtf32String(),
            CodePointLength = tokenCodePoints[index].Length,
            References = compressed.tokenReferences[index].Select(data => new LoadedInvertedIndex.TokenDocumentReference
            {
                DocumentId = data[0],
                Offsets = Enumerable.Range(0, data.Length / 2)
                    .Select(i => new OffsetSpan { Start = data[i * 2 + 1], End = data[i * 2 + 2] }).ToArray(),
            }).ToArray(),
        }).ToArray();

        return new LoadedInvertedIndex
        {
            Documents = documents,
            DocumentCodePoints = documentCodePoints,
            TokenDefinitions = tokenDefinitions,
            Tries = new LoadedInvertedIndex.TypedTries
            {
                Romaji = romajiTrie.Root,
                Kana = kanaTrie.Root,
                Other = otherTrie.Root,
            },
        };
    }
}
