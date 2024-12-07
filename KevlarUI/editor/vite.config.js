import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import { resolve } from 'node:path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  build: {
    watch: {
      include: 'src/**'
    },
    minify: true,
    lib: {
      entry: resolve(__dirname, 'src/main.js'),
      name: 'KevlarEditor',
      fileName: 'kevlar-editor'
    },
    emptyOutDir: true,
    outDir: 'dist/assets',
    rollupOptions: {
      output: {
        assetFileNames: 'kevlar-editor.[ext]',
        entryFileNames: 'kevlar-editor.js',
        format: 'esm',
        exports: 'named',
        inlineDynamicImports: true
      }
    }
  }
})
