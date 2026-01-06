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

public sealed class Search_BundleDocumentsOption_WorksWhenNotBundledTest : NeedleTestBase
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
        var compressed = InvertedIndexBuilder.BuildInvertedIndex(
            TestDocuments,
            TokenizerOptions,
            new InvertedIndexBuilderOptions { BundleDocuments = false });

        // Documents should not be in the compressed index
        Assert.Null(compressed.documents);

        // Load with documents provided explicitly
        var invertedIndex = InvertedIndexLoader.Load(compressed, TestDocuments);

        var results = InvertedIndexSearcher.Search(invertedIndex, "yoi");
        Assert.Contains("宵の鳥", results.Select(r => r.DocumentText));
    }
}

public sealed class Search_BundleDocumentsOption_ThrowsWhenNoneProvidedTest : NeedleTestBase
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
        var compressed = InvertedIndexBuilder.BuildInvertedIndex(
            TestDocuments,
            TokenizerOptions,
            new InvertedIndexBuilderOptions { BundleDocuments = false });

        Assert.Throws<ArgumentException>(() => InvertedIndexLoader.Load(compressed));
    }
}

public sealed class Search_FilterDocumentOption_ExcludesFilteredDocumentsTest : NeedleTestBase
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

        // Search without filter - should find "宵の鳥" (documentId 2)
        var resultsWithoutFilter = InvertedIndexSearcher.Search(invertedIndex, "yoi");
        Assert.Contains("宵の鳥", resultsWithoutFilter.Select(r => r.DocumentText));

        // Search with filter excluding documentId 2
        var resultsWithFilter = InvertedIndexSearcher.Search(invertedIndex, "yoi", new InvertedIndexSearcherOptions
        {
            FilterDocument = id => id != 2
        });
        Assert.DoesNotContain("宵の鳥", resultsWithFilter.Select(r => r.DocumentText));
    }
}

public sealed class Search_NextComparerOption_UsesCustomComparerTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        // Create documents that would have similar match scores
        var similarDocs = new[] { "テストA", "テストB", "テストC" };
        var compressed = InvertedIndexBuilder.BuildInvertedIndex(similarDocs, TokenizerOptions);
        var invertedIndex = InvertedIndexLoader.Load(compressed);

        // Search with reverse order comparer
        var results = InvertedIndexSearcher.Search(invertedIndex, "テスト", new InvertedIndexSearcherOptions
        {
            NextComparer = (a, b) => b - a // Reverse by documentId
        });

        // Should be in reverse documentId order (2, 1, 0) when other criteria equal
        Assert.Equal([2, 1, 0], results.Select(r => r.DocumentId).ToArray());
    }
}

