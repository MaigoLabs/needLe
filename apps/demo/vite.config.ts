import { defineConfig } from 'vite'
import UnoCSS from 'unocss/vite'
import react from '@vitejs/plugin-react'
import topLevelAwait from 'vite-plugin-top-level-await'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), UnoCSS(), topLevelAwait()],
  build: {
    assetsInlineLimit: 0,
    minify: true
  },
})
