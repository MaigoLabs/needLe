import { defineConfig } from 'tsdown';

export default defineConfig({
  entry: [
    './src/index.ts',
    './src/searcher/index.ts',
    './src/indexer/index.ts',
    './src/common/index.ts',
  ],
  dts: true,
  unused: true,
  fixedExtension: true,
  unbundle: true,
  sourcemap: true,
});
