using MaigoLabs.NeedLe.Indexer.Han;

namespace MaigoLabs.NeedLe.Tests.Indexer.Han;

#region IsHanCharacter

public sealed class IsHanCharacter_ReturnsTrueForCjkCharactersTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.True(HanVariantProvider.IsHanCharacter('中'));
        Assert.True(HanVariantProvider.IsHanCharacter('国'));
        Assert.True(HanVariantProvider.IsHanCharacter('日'));
        Assert.True(HanVariantProvider.IsHanCharacter('本'));
    }
}

public sealed class IsHanCharacter_ReturnsFalseForNonCjkCharactersTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.False(HanVariantProvider.IsHanCharacter('a'));
        Assert.False(HanVariantProvider.IsHanCharacter('あ'));
        Assert.False(HanVariantProvider.IsHanCharacter('ア'));
        Assert.False(HanVariantProvider.IsHanCharacter('1'));
    }
}

#endregion

#region GetHanVariants

public sealed class GetHanVariants_ReturnsVariantsForSimplifiedTraditionalTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var provider = new HanVariantProvider();
        // 国 (simplified) and 國 (traditional) should be variants of each other
        var variants1 = provider.GetHanVariants('国');
        var variants2 = provider.GetHanVariants('國');
        Assert.Contains('国', variants1);
        Assert.Contains('國', variants1);
        Assert.Contains('国', variants2);
        Assert.Contains('國', variants2);
    }
}

public sealed class GetHanVariants_ReturnsCharacterItselfForNoVariantsTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var provider = new HanVariantProvider();
        var variants = provider.GetHanVariants('一');
        Assert.Contains('一', variants);
    }
}

public sealed class GetHanVariants_ReturnsEmptyForNonHanCharactersTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var provider = new HanVariantProvider();
        Assert.Empty(provider.GetHanVariants('a'));
        Assert.Empty(provider.GetHanVariants('あ'));
    }
}

#endregion


