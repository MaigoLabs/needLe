using MaigoLabs.NeedLe.Common;

namespace MaigoLabs.NeedLe.Tests.Common;

#region ToKatakana

public sealed class ToKatakana_ConvertsHiraganaToKatakanaTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal("アイウエオ", CommonNormalization.ToKatakana("あいうえお"));
        Assert.Equal("カキクケコ", CommonNormalization.ToKatakana("かきくけこ"));
        Assert.Equal("サシスセソ", CommonNormalization.ToKatakana("さしすせそ"));
    }
}

public sealed class ToKatakana_KeepsKatakanaUnchangedTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal("アイウエオ", CommonNormalization.ToKatakana("アイウエオ"));
    }
}

public sealed class ToKatakana_KeepsNonKanaUnchangedTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal("abc123", CommonNormalization.ToKatakana("abc123"));
        Assert.Equal("漢字", CommonNormalization.ToKatakana("漢字"));
    }
}

public sealed class ToKatakana_HandlesMixedInputTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal("アアa漢", CommonNormalization.ToKatakana("あアa漢"));
    }
}

#endregion

#region NormalizeCodePoint

public sealed class NormalizeCodePoint_ConvertsFullwidthAsciiToHalfwidthLowercaseTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal('a', CommonNormalization.NormalizeCodePoint('Ａ'));
        Assert.Equal('b', CommonNormalization.NormalizeCodePoint('Ｂ'));
        Assert.Equal('c', CommonNormalization.NormalizeCodePoint('Ｃ'));
        Assert.Equal('1', CommonNormalization.NormalizeCodePoint('１'));
        Assert.Equal('2', CommonNormalization.NormalizeCodePoint('２'));
        Assert.Equal('3', CommonNormalization.NormalizeCodePoint('３'));
        Assert.Equal('!', CommonNormalization.NormalizeCodePoint('！'));
    }
}

public sealed class NormalizeCodePoint_ConvertsFullwidthSpaceToHalfwidthTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal(' ', CommonNormalization.NormalizeCodePoint('　'));
    }
}

public sealed class NormalizeCodePoint_ConvertsHalfwidthKanaToFullwidthTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal('ア', CommonNormalization.NormalizeCodePoint('ｱ'));
        Assert.Equal('イ', CommonNormalization.NormalizeCodePoint('ｲ'));
        Assert.Equal('ウ', CommonNormalization.NormalizeCodePoint('ｳ'));
        Assert.Equal('エ', CommonNormalization.NormalizeCodePoint('ｴ'));
        Assert.Equal('オ', CommonNormalization.NormalizeCodePoint('ｵ'));
        Assert.Equal('カ', CommonNormalization.NormalizeCodePoint('ｶ'));
    }
}

public sealed class NormalizeCodePoint_NormalizesVoicedSoundMarksTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal(0x3099, CommonNormalization.NormalizeCodePoint('ﾞ')); // halfwidth voiced -> combining
        Assert.Equal(0x309A, CommonNormalization.NormalizeCodePoint('ﾟ')); // halfwidth semi-voiced -> combining
        Assert.Equal(0x3099, CommonNormalization.NormalizeCodePoint('゛')); // fullwidth voiced -> combining
        Assert.Equal(0x309A, CommonNormalization.NormalizeCodePoint('゜')); // fullwidth semi-voiced -> combining
    }
}

public sealed class NormalizeCodePoint_ConvertsHalfwidthPunctuationToFullwidthTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal('。', CommonNormalization.NormalizeCodePoint('｡'));
        Assert.Equal('「', CommonNormalization.NormalizeCodePoint('｢'));
        Assert.Equal('」', CommonNormalization.NormalizeCodePoint('｣'));
        Assert.Equal('、', CommonNormalization.NormalizeCodePoint('､'));
        Assert.Equal('・', CommonNormalization.NormalizeCodePoint('･'));
    }
}

public sealed class NormalizeCodePoint_LowercasesRegularAsciiTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        Assert.Equal('a', CommonNormalization.NormalizeCodePoint('A'));
        Assert.Equal('b', CommonNormalization.NormalizeCodePoint('B'));
        Assert.Equal('c', CommonNormalization.NormalizeCodePoint('C'));
    }
}

#endregion


