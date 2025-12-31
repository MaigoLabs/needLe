using MaigoLabs.NeedLe.Common;

namespace MaigoLabs.NeedLe.Indexer.Trie;

public static class TrieSerializer
{
    private class NodeEntry
    {
        public int Id { get; set; }
        public bool Visited { get; set; }
        public int[]? Data { get; set; }
    }

    public static int[] Serialize(TrieNode root)
    {
        var nodeEntries = new Dictionary<TrieNode, NodeEntry>();
        var currentId = 0;
        NodeEntry GetNodeEntry(TrieNode node) => nodeEntries.TryGetValue(node, out var nodeEntry) ? nodeEntry :
            nodeEntries[node] = new NodeEntry { Id = ++currentId, Visited = false, Data = null };
        int SerializeNode(TrieNode node)
        {
            var entry = GetNodeEntry(node);
            if (entry.Visited) return entry.Id;
            entry.Visited = true;
            var children = node.Children.Select(child => (CodePoint: child.Key, ChildId: SerializeNode(child.Value))).ToArray();
            entry.Data =
            [
                node.Parent != null ? GetNodeEntry(node.Parent).Id : 0,
                .. children.Select(child => child.CodePoint),
                .. children.Select(child => child.ChildId),
                // End of children list (<= 0 are not valid code points nor node IDs)
                .. node.TokenIds.Count > 0
                    ? node.TokenIds.Select(tokenId => -(tokenId + 1)) // Use the negative value of (tokenId + 1)
                    : [0], // End of children list, no token IDs (token IDs are encoded to negative values)
            ];
            return entry.Id;
        }
        SerializeNode(root);
        return nodeEntries.Values.OrderBy(entry => entry.Id).SelectMany(entry => entry.Data ?? []).ToArray();
    }
}
