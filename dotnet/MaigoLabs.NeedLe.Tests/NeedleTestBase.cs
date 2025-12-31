using MaigoLabs.NeedLe.Indexer;
using MaigoLabs.NeedLe.Indexer.Han;
using MaigoLabs.NeedLe.Indexer.Japanese;

namespace MaigoLabs.NeedLe.Tests;

public abstract class NeedleTestBase
{
    public static HanVariantProvider HanVariantProvider { get; set; } = new();
    public static TranscriptionProvider TranscriptionProvider { get; set; } = new();
    public static TokenizerOptions TokenizerOptions => new() { HanVariantProvider = HanVariantProvider, TranscriptionProvider = TranscriptionProvider };
}
