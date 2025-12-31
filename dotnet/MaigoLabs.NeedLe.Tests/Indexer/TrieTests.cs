using MaigoLabs.NeedLe.Common;
using MaigoLabs.NeedLe.Common.Extensions;
using MaigoLabs.NeedLe.Indexer.Trie;

namespace MaigoLabs.NeedLe.Tests.Indexer;

#region GraftTriePaths

public sealed class GraftTriePaths_GraftsPathsAccordingToNormalizationRulesTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        // Build a trie with tokens containing normalized forms
        var trie = TrieBuilder.BuildTrie([
            (0, "sya".ToCodePoints()), // normalized form of "sha"
            (1, "tu".ToCodePoints()),  // normalized form of "tsu"
        ]);

        // Graft paths so that "sha" -> "sya" and "tsu" -> "tu"
        TrieBuilder.GraftTriePaths(trie, [
            ("sha".ToCodePoints().ToArray(), "sya".ToCodePoints().ToArray()),
            ("tsu".ToCodePoints().ToArray(), "tu".ToCodePoints().ToArray()),
        ]);

        // Now we should be able to traverse using both the original and grafted paths
        var syaNode = trie.Traverse("sya".ToCodePoints().ToArray());
        var shaNode = trie.Traverse("sha".ToCodePoints().ToArray());
        Assert.NotNull(syaNode);
        Assert.NotNull(shaNode);
        Assert.Same(syaNode, shaNode); // Both paths should lead to the same node

        var tuNode = trie.Traverse("tu".ToCodePoints().ToArray());
        var tsuNode = trie.Traverse("tsu".ToCodePoints().ToArray());
        Assert.NotNull(tuNode);
        Assert.NotNull(tsuNode);
        Assert.Same(tuNode, tsuNode);
    }
}

public sealed class GraftTriePaths_HandlesChainedGraftRulesTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var trie = TrieBuilder.BuildTrie([
            (0, "o".ToCodePoints()), // normalized vowel
        ]);

        // Chain: "ou" -> "o", "oo" -> "o"
        TrieBuilder.GraftTriePaths(trie, [
            ("ou".ToCodePoints().ToArray(), "o".ToCodePoints().ToArray()),
            ("oo".ToCodePoints().ToArray(), "o".ToCodePoints().ToArray()),
        ]);

        var oNode = trie.Traverse("o".ToCodePoints().ToArray());
        var ouNode = trie.Traverse("ou".ToCodePoints().ToArray());
        var ooNode = trie.Traverse("oo".ToCodePoints().ToArray());

        Assert.NotNull(oNode);
        Assert.Same(oNode, ouNode);
        Assert.Same(oNode, ooNode);
    }
}

#endregion
