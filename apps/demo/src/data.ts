import { buildInvertedIndex } from '@maigolabs/needle/indexer';
import { loadInvertedIndex } from '@maigolabs/needle/searcher';
import { TokenizerBuilder } from '@patdx/kuromoji';

// Indexer loads OpenCC and pinyin-pro which is large, put them in data.ts for dynamic importing.
export { createTokenizer } from '@maigolabs/needle/indexer';

const musicNames: string[] = [...new Set(
  Object.values(
    await (await fetch('https://sekai-world.github.io/sekai-master-db-diff/musics.json')).json(),
  ).map(music => (music as { title: string }).title),
)];

export const kuromoji = await new TokenizerBuilder({
  loader: {
    loadArrayBuffer: async (url: string) => {
      url = `https://cdn.jsdelivr.net/npm/@aiktb/kuromoji@1.0.2/dict/${url.replace('.gz', '')}`;
      const res = await fetch(url);
      if (!res.ok) throw new Error(`Failed to fetch ${url}`);
      return await res.arrayBuffer();
    },
  },
}).build();

export const compressed = buildInvertedIndex(musicNames, { kuromoji });
export const invertedIndex = loadInvertedIndex(compressed);
