using MaigoLabs.NeedLe.Indexer.Japanese;

namespace MaigoLabs.NeedLe.Tests.Indexer.Japanese;

public sealed class GetAllKanaReadings_ReturnsKatakanaForPureKanaInputTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var provider = new TranscriptionProvider();
        var readings = provider.GetAllKanaReadings("あ");
        Assert.Contains("ア", readings);
    }
}

public sealed class GetAllKanaReadings_ReturnsReadingsForKanjiTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var provider = new TranscriptionProvider();
        var readings = provider.GetAllKanaReadings("僕");
        Assert.NotEmpty(readings);
        // 僕 should have reading ボク
        Assert.Contains("ボク", readings);
    }
}

public sealed class GetAllKanaReadings_ReturnsReadingsForCompoundWordsTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var provider = new TranscriptionProvider();
        var readings = provider.GetAllKanaReadings("和風");
        Assert.NotEmpty(readings);
    }
}


