using MaigoLabs.NeedLe.Common.Extensions;
using OpenccNetLib;

namespace MaigoLabs.NeedLe.Indexer.Han;

public class HanVariantProvider
{
    private readonly Dictionary<int, int[]> EXCHANGE_MAP;
    public HanVariantProvider(DictWithMaxLength[]? dicts = null)
    {
        dicts ??=
        [
            DictionaryLib.Provider.hk_variants,
            DictionaryLib.Provider.hk_variants_rev,
            DictionaryLib.Provider.jp_variants,
            DictionaryLib.Provider.jp_variants_rev,
            DictionaryLib.Provider.st_characters,
            DictionaryLib.Provider.ts_characters,
            DictionaryLib.Provider.tw_variants,
            DictionaryLib.Provider.tw_variants_rev,
        ];
        EXCHANGE_MAP = BuildHanExchangeMap(dicts);
    }

    private Dictionary<int, int[]> BuildHanExchangeMap(DictWithMaxLength[] dicts)
    {
        var unionFindSet = new UnionFindSet();
        foreach (var dict in dicts) foreach (var item in dict.Dict)
        {
            var from = item.Key.ToCodePoints().ToArray();
            var to = item.Value.ToCodePoints().ToArray();
            if (from.Length != 1 || to.Length != 1) continue;
            unionFindSet.Union(from[0], to[0]);
        }
        var variants = new Dictionary<int, List<int>>();
        foreach (var x in unionFindSet.Keys)
        {
            var parent = unionFindSet.Find(x);
            if (!variants.TryGetValue(parent, out var list)) variants[parent] = list = [];
            if (x != parent) variants[x] = list;
            list.Add(x);
        }
        return variants.ToDictionary(item => item.Key, item => item.Value.OrderBy(x => x).ToArray());
    }

    // https://github.com/google/re2/blob/e7aec5985072c1dbe735add802653ef4b36c231a/re2/unicode_groups.cc#L5590-L5615
    private static readonly (int Min, int Max)[] RE2_SCRIPT_HAN_RENAGES =
    [
        // Han_range16
        (11904, 11929),
        (11931, 12019),
        (12032, 12245),
        (12293, 12293),
        (12295, 12295),
        (12321, 12329),
        (12344, 12347),
        (13312, 19903),
        (19968, 40959),
        (63744, 64109),
        (64112, 64217),
        // Han_range32
        (94178, 94179),
        (94192, 94193),
        (131072, 173791),
        (173824, 177977),
        (177984, 178205),
        (178208, 183969),
        (183984, 191456),
        (191472, 192093),
        (194560, 195101),
        (196608, 201546),
        (201552, 205743),
    ];

    public static bool IsHanCharacter(int codePoint) => RE2_SCRIPT_HAN_RENAGES.Any(range => codePoint >= range.Min && codePoint <= range.Max);

    public int[] GetHanVariants(int codePoint) => EXCHANGE_MAP.TryGetValue(codePoint, out var variants)
        ? variants
        : IsHanCharacter(codePoint) ? [codePoint] : [];
}
