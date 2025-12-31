using MaigoLabs.NeedLe.Common;

namespace MaigoLabs.NeedLe.Searcher.Trie;

public class DeserializedTrie
{
    public required TrieNode Root { get; set; }
    public required Dictionary<int, int[]> TokenCodePoints { get; set; }
}

public static class TrieDeserializer
{
    public static DeserializedTrie Deserialize(int[] data)
    {
        var nodes = new List<TrieNode?>();
        TrieNode GetNode(int id)
        {
            if (id > nodes.Count) nodes.AddRange(Enumerable.Repeat<TrieNode?>(null, id - nodes.Count));
            return nodes[id - 1] ??= new TrieNode { Parent = null, Children = [], TokenIds = [], SubTreeTokenIds = [] };
        }
        var currentId = 0;
        for (var i = 0; i < data.Length; )
        {
            var node = GetNode(++currentId);
            var parentId = data[i++];
            node.Parent = parentId != 0 ? GetNode(parentId) : null;

            var endOfChildren = i;
            while (endOfChildren < data.Length && data[endOfChildren] > 0) endOfChildren++;
            var numberOfChildren = (endOfChildren - i) / 2;
            for (var j = i; j < i + numberOfChildren; j++)
            {
                var codePoint = data[j];
                var child = GetNode(data[j + numberOfChildren]);
                node.Children.Add(codePoint, child);
            }
            i = endOfChildren;

            if (data[i] == 0) i++; // No token IDs
            else while (i < data.Length && data[i] < 0) node.TokenIds.Add(-data[i++] - 1);
        }
        var root = nodes[0]!;

        // DFS to construct code point paths for each token
        var tokenCodePoints = new Dictionary<int, int[]>();
        var currentCodePoints = new List<int>();
        void DfsCodePoints(TrieNode node)
        {
            foreach (var tokenId in node.TokenIds) tokenCodePoints.Add(tokenId, [.. currentCodePoints]);
            foreach (var (codePoint, child) in node.Children)
            {
                if (child.Parent != node) continue; // Skip grafted paths as these are not the canonical representation of the tokens
                currentCodePoints.Add(codePoint);
                DfsCodePoints(child);
                currentCodePoints.RemoveAt(currentCodePoints.Count - 1);
            }
        }
        DfsCodePoints(root);

        // DFS to construct subTreeTokenIds for each node
        var visitedNodes = new HashSet<TrieNode>();
        List<int> DfsSubTreeTokenIds(TrieNode node)
        {
            if (visitedNodes.Contains(node)) return node.SubTreeTokenIds;
            visitedNodes.Add(node);
            node.SubTreeTokenIds = new HashSet<int>(node.TokenIds.Concat(node.Children.Values.SelectMany(DfsSubTreeTokenIds))).ToList();
            return node.SubTreeTokenIds;
        };
        DfsSubTreeTokenIds(root);

        return new DeserializedTrie { Root = root, TokenCodePoints = tokenCodePoints };
    }
}
