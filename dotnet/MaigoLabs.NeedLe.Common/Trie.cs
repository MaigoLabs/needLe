namespace MaigoLabs.NeedLe.Common;

public class TrieNode
{
    public required TrieNode? Parent { get; set; }
    public required Dictionary<int, TrieNode> Children { get; set; } // Unicode code point -> child node
    public required List<int> TokenIds { get; set; }
    public required List<int> SubTreeTokenIds { get; set; } // Empty on root.
}

public static class TrieNodeExtensions
{
    public static TrieNode? TraverseStep(this TrieNode? node, int codePoint, bool isIgnorable = false) =>
        (node?.Children.TryGetValue(codePoint, out var child) ?? false)
            ? child
            : isIgnorable ? node : null;

    public static TrieNode? Traverse(this TrieNode? node, int[] codePoints, bool isIgnorable = false)
    {
        if (node == null) return null;
        foreach (var codePoint in codePoints)
        {
            node = node?.TraverseStep(codePoint, isIgnorable);
            if (node == null) return null;
        }
        return node;
    }

    public static List<int> GetTokenIds(this TrieNode? node, bool includeSubTree = false) =>
        (includeSubTree ? node?.SubTreeTokenIds : node?.TokenIds) ?? [];

    public static bool IsTokenExactMatch(this TrieNode? node, int tokenId) => node?.TokenIds.Contains(tokenId) ?? false;
}
