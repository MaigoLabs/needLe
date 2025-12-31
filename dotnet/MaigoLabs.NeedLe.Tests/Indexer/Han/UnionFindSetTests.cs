using MaigoLabs.NeedLe.Indexer.Han;

namespace MaigoLabs.NeedLe.Tests.Indexer.Han;

public sealed class UnionFindSet_FindsSelfAsRootInitiallyTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var ufs = new UnionFindSet();
        Assert.Equal(1, ufs.Find(1));
        Assert.Equal(2, ufs.Find(2));
    }
}

public sealed class UnionFindSet_UnionsTwoElementsTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var ufs = new UnionFindSet();
        ufs.Union(1, 2);
        Assert.Equal(ufs.Find(1), ufs.Find(2));
    }
}

public sealed class UnionFindSet_UnionsMultipleElementsTransitivelyTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var ufs = new UnionFindSet();
        ufs.Union(1, 2);
        ufs.Union(2, 3);
        ufs.Union(4, 5);
        Assert.Equal(ufs.Find(1), ufs.Find(3));
        Assert.NotEqual(ufs.Find(1), ufs.Find(4));
        ufs.Union(3, 4);
        Assert.Equal(ufs.Find(1), ufs.Find(5));
    }
}

public sealed class UnionFindSet_IteratesAllKeysTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var ufs = new UnionFindSet();
        ufs.Union(1, 2);
        ufs.Union(3, 4);
        var keys = ufs.Keys.ToList();
        Assert.Contains(1, keys);
        Assert.Contains(2, keys);
        Assert.Contains(3, keys);
        Assert.Contains(4, keys);
    }
}


