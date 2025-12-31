using MaigoLabs.NeedLe.Common;

namespace MaigoLabs.NeedLe.Indexer.Trie;

public static class TrieBuilder
{
    private static TrieNode NewNode(TrieNode? parent) => new() { Parent = parent, Children = [], TokenIds = [], SubTreeTokenIds = [] };

    public static TrieNode BuildTrie(IEnumerable<(int Id, IEnumerable<int> CodePoints)> tokens)
    {
        var root = NewNode(null);
        foreach (var (id, codePoints) in tokens)
        {
            var node = root;
            foreach (var codePoint in codePoints)
            {
                node.Children.TryGetValue(codePoint, out var childNode);
                if (childNode == null) node.Children[codePoint] = childNode = NewNode(node);
                node = childNode;
                node.SubTreeTokenIds.Add(id);
            }
            node.TokenIds.Add(id);
        }
        return root;
    }

    public static void GraftTriePaths(TrieNode root, IEnumerable<(int[] From, int[] To)> rules)
    {
        foreach (var (inputPhrase, graftTo) in rules) if (graftTo.Length > inputPhrase.Length) throw new ArgumentException($"Graft rule {inputPhrase} -> {graftTo} maps to longer string and may cause infinite loop");
        var visitedNodes = new HashSet<TrieNode>();
        void GraftFromNode(TrieNode node, bool recursiveChildren)
        {
            if (!visitedNodes.Add(node)) return;
            if (recursiveChildren) foreach (var child in node.Children.Values) GraftFromNode(child, true);
            while (true)
            {
                var nodesWithNewGraftedChildren = new Dictionary<TrieNode, /* depth from initial node */ int>();
                foreach (var (inputPhrase, graftTo) in rules)
                {
                    var targetNode = node.Traverse(graftTo);
                    if (targetNode == null) continue;
                    var graftedPath = new TrieNode[inputPhrase.Length - 1];
                    var isGrafted = false;
                    var currentNode = node;
                    for (var i = 0; i < inputPhrase.Length; i++)
                    {
                        var codePoint = inputPhrase[i];
                        currentNode.Children.TryGetValue(codePoint, out var childNode);
                        if (i == inputPhrase.Length - 1)
                        {
                            if (childNode != null)
                            {
                                if (childNode != targetNode) throw new ArgumentException($"Grafted path {inputPhrase} conflicts with existing path");
                                // Already grafted
                            }
                            else
                            {
                                currentNode.Children[codePoint] = childNode = targetNode;
                                isGrafted = true;
                            }
                        }
                        else
                        {
                            if (childNode == null)
                            {
                                childNode = NewNode(currentNode);
                                childNode.SubTreeTokenIds = targetNode.SubTreeTokenIds;
                                currentNode.Children[codePoint] = childNode;
                            }
                            else
                            {
                                // Part of another grafted path?
                                childNode.SubTreeTokenIds = new HashSet<int>(childNode.SubTreeTokenIds.Concat(targetNode.SubTreeTokenIds)).ToList();
                            }
                            graftedPath[i] = currentNode = childNode;
                        }
                    }
                    if (isGrafted) for (var i = 0; i < graftedPath.Length; i++) nodesWithNewGraftedChildren[graftedPath[i]!] = i + 1;
                }
                if (nodesWithNewGraftedChildren.Count > 0)
                {
                    // Re-check graft rules on the newly grafted path
                    // 1. No need to recursive other children (not on this path) since their children are not affected
                    // 2. No need to consider ancestors of this node since they're handled later (we run in DFS order)
                    var sortedNodes = nodesWithNewGraftedChildren.OrderByDescending(x => x.Value);
                    foreach (var (changedNode, _) in sortedNodes) GraftFromNode(changedNode, false);
                }
                else break; // No new grafts applied
            }
        }
        GraftFromNode(root, true);
    }
}
