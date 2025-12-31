using System.Runtime.InteropServices;
using MaigoLabs.NeedLe.Common;
using MaigoLabs.NeedLe.Common.Extensions;
using MeCab;
using MeCab.Core;

namespace MaigoLabs.NeedLe.Indexer.Japanese;

public class Transcription
{
    public required int Start { get; set; }
    public required int Length { get; set; }
    public required string[] Transcriptions { get; set; }
}

public delegate IEnumerable<Transcription> TranscriptionEnumerator(ReadOnlyMemory<int> codePoints);
public delegate bool IsValidPhraseDelegate(ReadOnlyMemory<int> codePoints, int start, int length);
public delegate HashSet<string> GetAllTranscriptionsDelegate(string phrase);

public class TranscriptionProvider
{
    public MeCabDictionary[] Dictionaries { get; set; }

    public TranscriptionProvider(MeCabDictionary[]? dictionaries = null)
    {
        if (dictionaries == null)
        {
            var param = new MeCabParam();
            param.LoadDicRC();
            var dictionary = new MeCabDictionary();
            dictionary.Open(Path.Combine(param.DicDir, "sys.dic"));
            dictionaries = [dictionary];
        }
        Dictionaries = dictionaries;
    }

    public static TranscriptionEnumerator CreateTranscriptionEnumerator(IsValidPhraseDelegate isValidPhrase, GetAllTranscriptionsDelegate getAllTranscriptions) => codePoints =>
    {
        var resultMap = new Dictionary<(int Start, int Length), Transcription>();
        for (int phraseLength = 1; phraseLength <= codePoints.Length; phraseLength++) for (int start = 0; start + phraseLength <= codePoints.Length; start++)
        {
            if (!isValidPhrase(codePoints, start, phraseLength)) continue;
            var phrase = MemoryMarshal.ToEnumerable(codePoints.Slice(start, phraseLength)).ToUtf32String();
            var atomicTranscriptions = getAllTranscriptions(phrase).Where(transcription => transcription != null).Where(candidateTranscription =>
            {
                if (candidateTranscription.Length == 0) return false;
                // Ensure the transcription is atomic (not a combination of multiple shorter transcriptions, separated by any midpoints)
                var visitedStates = new HashSet<(int PhrasePosition, int TranscriptionPosition)>();
                var queue = new Queue<(int PhrasePosition, int TranscriptionPosition)>();
                queue.Enqueue((0, 0));
                while (queue.Count > 0)
                {
                    var (phrasePosition, transcriptionPosition) = queue.Dequeue();
                    for (int prefixLength = 1; prefixLength <= phraseLength - phrasePosition; prefixLength++)
                    {
                        if (!resultMap.TryGetValue((start + phrasePosition, prefixLength), out var prefixResult)) continue;
                        foreach (var transcription in prefixResult.Transcriptions) if (string.Compare(candidateTranscription, transcriptionPosition, transcription, 0, transcription.Length) == 0)
                        {
                            var nextState = (PhrasePosition: phrasePosition + prefixLength, TranscriptionPosition: transcriptionPosition + transcription.Length);
                            if (nextState.PhrasePosition == phraseLength && nextState.TranscriptionPosition == candidateTranscription.Length) return false; // Found a valid combination
                            if (visitedStates.Contains(nextState)) continue;
                            visitedStates.Add(nextState);
                            queue.Enqueue(nextState);
                        }
                    }
                }
                return true;
            }).ToArray();
            if (atomicTranscriptions.Length > 0) resultMap[(start, phraseLength)] = new() { Start = start, Length = phraseLength, Transcriptions = atomicTranscriptions };
        }
        return resultMap.Values;
    };

    public HashSet<string> GetAllKanaReadings(string phrase)
    {
        var result = new HashSet<string>();
        var isKana = phrase.All(ch => JapaneseUtils.IsKana(ch));
        if (isKana) result.Add(CommonNormalization.ToKatakana(phrase));
        if (isKana && phrase.Length == 1) return result;

        foreach (var dictionary in Dictionaries)
        {
            var searchResult = dictionary.ExactMatchSearch(phrase);
            if (searchResult.Value == -1) continue;
            var tokens = dictionary.GetToken(searchResult);
            foreach (var token in tokens)
            {
                var feature = dictionary.GetFeature(token.Feature);
                var parts = feature.Split(',');
                if (parts.Length > 7) result.Add(CommonNormalization.ToKatakana(parts[7]));
            }
        }
        return result;
    }

    public HashSet<string> GetAllKanaReadingsWithNormalization(string phrase) =>
        GetAllKanaReadings(JapaneseUtils.StripJapaneseSoundMarks(JapaneseNormalization.NormalizeKanaDakuten(phrase)));

    public TranscriptionEnumerator EnumerateKanaTranscriptions => CreateTranscriptionEnumerator(
        JapaneseUtils.IsValidJapanesePhrase,
        GetAllKanaReadingsWithNormalization);
    public TranscriptionEnumerator EnumerateRomajiTranscriptions => CreateTranscriptionEnumerator(
        JapaneseUtils.IsValidJapanesePhrase,
        phrase => [.. GetAllKanaReadingsWithNormalization(phrase).Select(kana => JapaneseNormalization.NormalizeRomaji(JapaneseUtils.ToRomajiStrictly(kana)))]);
}
