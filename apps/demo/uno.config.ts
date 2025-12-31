import { createLocalFontProcessor } from '@unocss/preset-web-fonts/local';
import { defineConfig, presetWind3, presetTypography, presetWebFonts, transformerVariantGroup, transformerDirectives, presetIcons } from 'unocss';

export default defineConfig({
  presets: [
    presetWind3(),
    presetTypography(),
    presetIcons({
      scale: 1.2,
      warn: true,
    }),
    presetWebFonts({
      fonts: {
        mono: {
          name: 'Maple Mono',
          provider: 'fontsource',
        },
      },
      processors: createLocalFontProcessor({
        cacheDir: 'node_modules/.cache/unocss/fonts',
        fontAssetsDir: 'public/assets/fonts/cache',
        fontServeBaseUrl: '/assets/fonts/cache',
      }),
    }),
  ],
  transformers: [
    transformerDirectives(),
    transformerVariantGroup(),
  ],
});
