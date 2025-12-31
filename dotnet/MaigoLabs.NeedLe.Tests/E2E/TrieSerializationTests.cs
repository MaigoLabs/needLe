using MaigoLabs.NeedLe.Common;
using MaigoLabs.NeedLe.Common.Extensions;
using MaigoLabs.NeedLe.Indexer.Trie;
using MaigoLabs.NeedLe.Searcher.Trie;

namespace MaigoLabs.NeedLe.Tests.E2E;

#region Trie Building

public sealed class TrieBuilding_BuildsTrieWithMultipleDifferentTokensTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var trie = TrieBuilder.BuildTrie([
            (0, "hello".ToCodePoints()),
            (1, "help".ToCodePoints()),
            (2, "world".ToCodePoints()),
            (3, "word".ToCodePoints()),
        ]);

        // Traverse to verify structure
        var helloNode = trie.Traverse("hello".ToCodePoints().ToArray());
        var helpNode = trie.Traverse("help".ToCodePoints().ToArray());
        var worldNode = trie.Traverse("world".ToCodePoints().ToArray());
        var wordNode = trie.Traverse("word".ToCodePoints().ToArray());

        Assert.NotNull(helloNode);
        Assert.NotNull(helpNode);
        Assert.NotNull(worldNode);
        Assert.NotNull(wordNode);

        // Check token IDs
        Assert.Contains(0, helloNode!.TokenIds);
        Assert.Contains(1, helpNode!.TokenIds);
        Assert.Contains(2, worldNode!.TokenIds);
        Assert.Contains(3, wordNode!.TokenIds);

        // Check that 'hel' prefix node has both tokens in subTree
        var helNode = trie.Traverse("hel".ToCodePoints().ToArray());
        Assert.NotNull(helNode);
        Assert.Contains(0, helNode!.SubTreeTokenIds);
        Assert.Contains(1, helNode.SubTreeTokenIds);
    }
}

public sealed class TrieBuilding_HandlesJapaneseTextTokensTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var trie = TrieBuilder.BuildTrie([
            (0, "さくら".ToCodePoints()),
            (1, "サクラ".ToCodePoints()),
            (2, "桜".ToCodePoints()),
        ]);

        Assert.Contains(0, trie.Traverse("さくら".ToCodePoints().ToArray())?.TokenIds ?? []);
        Assert.Contains(1, trie.Traverse("サクラ".ToCodePoints().ToArray())?.TokenIds ?? []);
        Assert.Contains(2, trie.Traverse("桜".ToCodePoints().ToArray())?.TokenIds ?? []);
    }
}

#endregion

#region Trie Serialization

public sealed class TrieSerialization_SerializesAndDeserializesCorrectlyTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var originalTrie = TrieBuilder.BuildTrie([
            (0, "apple".ToCodePoints()),
            (1, "app".ToCodePoints()),
            (2, "banana".ToCodePoints()),
        ]);

        // Serialize
        var serialized = TrieSerializer.Serialize(originalTrie);
        Assert.True(serialized.Length > 0);

        // Deserialize
        var deserialized = TrieDeserializer.Deserialize(serialized);
        var deserializedTrie = deserialized.Root;
        var tokenCodePoints = deserialized.TokenCodePoints;

        // Verify structure is preserved
        var appleNode = deserializedTrie.Traverse("apple".ToCodePoints().ToArray());
        var appNode = deserializedTrie.Traverse("app".ToCodePoints().ToArray());
        var bananaNode = deserializedTrie.Traverse("banana".ToCodePoints().ToArray());

        Assert.NotNull(appleNode);
        Assert.NotNull(appNode);
        Assert.NotNull(bananaNode);

        Assert.Contains(0, appleNode!.TokenIds);
        Assert.Contains(1, appNode!.TokenIds);
        Assert.Contains(2, bananaNode!.TokenIds);

        // Verify tokenCodePoints map
        Assert.Equal("apple", tokenCodePoints[0].ToUtf32String());
        Assert.Equal("app", tokenCodePoints[1].ToUtf32String());
        Assert.Equal("banana", tokenCodePoints[2].ToUtf32String());

        // Verify subTreeTokenIds are reconstructed
        Assert.Contains(0, appNode.SubTreeTokenIds);
        Assert.Contains(1, appNode.SubTreeTokenIds);
    }
}

public sealed class TrieSerialization_PreservesParentReferencesAfterDeserializationTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var originalTrie = TrieBuilder.BuildTrie([
            (0, "test".ToCodePoints()),
        ]);

        var serialized = TrieSerializer.Serialize(originalTrie);
        var deserialized = TrieDeserializer.Deserialize(serialized);
        var root = deserialized.Root;

        var testNode = root.Traverse("test".ToCodePoints().ToArray());
        Assert.NotNull(testNode);

        // Walk back to root via parent references
        TrieNode? node = testNode;
        var depth = 0;
        while (node?.Parent != null)
        {
            node = node.Parent;
            depth++;
        }
        Assert.Equal(4, depth); // 't' -> 'e' -> 's' -> 't' -> root
        Assert.Same(root, node);
    }
}

#endregion


