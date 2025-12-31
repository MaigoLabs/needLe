using MaigoLabs.NeedLe.Indexer.Han;

namespace MaigoLabs.NeedLe.Tests.Indexer.Han;

public sealed class GetPinyinCandidates_ReturnsPinyinForHanCharacterTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var candidates = PinyinHelper.GetPinyinCandidates('中').ToList();
        Assert.Contains("zhong", candidates);
        Assert.Contains("zh", candidates); // initial
        Assert.Contains("z", candidates); // first letter
    }
}

public sealed class GetPinyinCandidates_ReturnsMultiplePinyinForPolyphonicTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        // 行 can be "xing" or "hang"
        var candidates = PinyinHelper.GetPinyinCandidates('行').ToList();
        Assert.Contains("xing", candidates);
        Assert.Contains("hang", candidates);
    }
}

public sealed class GetPinyinCandidates_IncludesFuzzyPinyinVariantsTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        // 风 is "feng", should also have fuzzy variant "fen"
        var candidates = PinyinHelper.GetPinyinCandidates('风').ToList();
        Assert.Contains("feng", candidates);
        Assert.Contains("fen", candidates); // fuzzy: eng -> en
    }
}

public sealed class GetPinyinCandidates_ReturnsEmptyForNonHanCharactersTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Empty(PinyinHelper.GetPinyinCandidates('a'));
        Assert.Empty(PinyinHelper.GetPinyinCandidates('あ'));
    }
}


