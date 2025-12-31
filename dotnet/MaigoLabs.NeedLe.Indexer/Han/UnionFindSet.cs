namespace MaigoLabs.NeedLe.Indexer.Han;

public class UnionFindSet
{
    private Dictionary<int, int> Parent { get; set; } = [];
    private Dictionary<int, int> Rank { get; set; } = [];

    public IEnumerable<int> Keys => Parent.Keys;

    public int Find(int x)
    {
        if (!Parent.TryGetValue(x, out var parent)) return Parent[x] = x;
        else if (x == parent) return x;
        else return Parent[x] = Find(parent);
    }

    public void Union(int x, int y)
    {
        x = Find(x);
        y = Find(y);
        if (x == y) return;
        int rankX = GetRank(x), rankY = GetRank(y);
        if (rankX < rankY) Parent[x] = y;
        else if (rankX > rankY) Parent[y] = x;
        else
        {
            Parent[y] = x;
            Rank[x] = rankX + 1;
        }
    }

    private int GetRank(int x) => !Rank.TryGetValue(x, out var rank) ? 0 : rank;
}
