using MaigoLabs.NeedLe.Indexer;
using MaigoLabs.NeedLe.Searcher;

namespace MaigoLabs.NeedLe.Tests.E2E;

public sealed class Search_MatchesWithMixedSearchQueryTest : NeedleTestBase
{
    private static readonly string[] TestDocuments =
    [
        "ミーティア",
        "エンドマークに希望と涙を添えて",
        "宵の鳥",
        "僕の和風本当上手",
    ];

    [Fact]
    public void Execute()
    {
        var compressed = InvertedIndexBuilder.BuildInvertedIndex(TestDocuments, TokenizerOptions);
        var invertedIndex = InvertedIndexLoader.Load(compressed);

        var results = InvertedIndexSearcher.Search(invertedIndex, "bokunoh风じょう");

        // Should have at least one result
        Assert.NotEmpty(results);

        // The first result should be "僕の和風本当上手"
        Assert.Equal("僕の和風本当上手", results[0].DocumentText);
    }
}

public sealed class Search_HighlightsSearchResultCorrectlyTest : NeedleTestBase
{
    private static readonly string[] TestDocuments =
    [
        "ミーティア",
        "エンドマークに希望と涙を添えて",
        "宵の鳥",
        "僕の和風本当上手",
    ];

    [Fact]
    public void Execute()
    {
        var compressed = InvertedIndexBuilder.BuildInvertedIndex(TestDocuments, TokenizerOptions);
        var invertedIndex = InvertedIndexLoader.Load(compressed);

        var results = InvertedIndexSearcher.Search(invertedIndex, "bokunoh风じょう");
        Assert.NotEmpty(results);

        var highlighted = SearchResultHighlighter.Highlight(results[0]);

        // Should be a list of parts
        Assert.NotEmpty(highlighted);

        // Collect highlighted text
        var highlightedTexts = highlighted.Where(p => p.IsHighlighted).Select(p => p.Text).ToList();
        var highlightedJoined = string.Join("", highlightedTexts);

        Assert.Contains("僕", highlightedJoined);
        Assert.Contains("の", highlightedJoined);
        Assert.Contains("和", highlightedJoined);
        Assert.Contains("風", highlightedJoined);
        Assert.Contains("上", highlightedJoined);
    }
}

public sealed class Search_MatchesRomajiInputToKanaDocumentsTest : NeedleTestBase
{
    private static readonly string[] TestDocuments =
    [
        "ミーティア",
        "エンドマークに希望と涙を添えて",
        "宵の鳥",
        "僕の和風本当上手",
    ];

    [Fact]
    public void Execute()
    {
        var compressed = InvertedIndexBuilder.BuildInvertedIndex(TestDocuments, TokenizerOptions);
        var invertedIndex = InvertedIndexLoader.Load(compressed);

        // Search for "yoi" should match "宵の鳥"
        var results = InvertedIndexSearcher.Search(invertedIndex, "yoi");
        var matchedTexts = results.Select(r => r.DocumentText).ToList();

        Assert.Contains("宵の鳥", matchedTexts);
    }
}

