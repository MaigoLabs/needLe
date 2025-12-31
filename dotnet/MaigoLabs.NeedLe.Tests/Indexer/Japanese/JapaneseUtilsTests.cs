using MaigoLabs.NeedLe.Indexer.Japanese;

namespace MaigoLabs.NeedLe.Tests.Indexer.Japanese;

#region ToRomajiStrictly

public sealed class ToRomajiStrictly_ConvertsBasicKanaToRomajiTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal("a", JapaneseUtils.ToRomajiStrictly("あ"));
        Assert.Equal("ka", JapaneseUtils.ToRomajiStrictly("か"));
        Assert.Equal("sakura", JapaneseUtils.ToRomajiStrictly("さくら"));
    }
}

public sealed class ToRomajiStrictly_ConvertsKatakanaToRomajiTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal("a", JapaneseUtils.ToRomajiStrictly("ア"));
        Assert.Equal("ka", JapaneseUtils.ToRomajiStrictly("カ"));
        Assert.Equal("sakura", JapaneseUtils.ToRomajiStrictly("サクラ"));
    }
}

public sealed class ToRomajiStrictly_HandlesLongVowelsTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal("ou", JapaneseUtils.ToRomajiStrictly("おう"));
        Assert.Equal("oo", JapaneseUtils.ToRomajiStrictly("おお"));
    }
}

public sealed class ToRomajiStrictly_ReturnsEmptyForInvalidFirstCharacterTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal("", JapaneseUtils.ToRomajiStrictly("ー")); // prolonged sound mark cannot be first
        Assert.Equal("", JapaneseUtils.ToRomajiStrictly("ゃ")); // small ya cannot be first
    }
}

public sealed class ToRomajiStrictly_ReturnsEmptyForInvalidLastCharacterTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal("", JapaneseUtils.ToRomajiStrictly("っ")); // small tsu cannot be last
    }
}

public sealed class ToRomajiStrictly_HandlesGeminationTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal("katta", JapaneseUtils.ToRomajiStrictly("かった"));
    }
}

#endregion


