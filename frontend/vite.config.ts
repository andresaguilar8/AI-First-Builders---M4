import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'
import { defineConfig } from 'vitest/config'

export default defineConfig({
  plugins: [
    react(),
    // Instalable en Chrome, Edge y Chrome Android (RNF-03). Offline está fuera de alcance: el
    // service worker solo guarda los archivos de la app y nunca los datos de /api.
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.svg', 'apple-touch-icon.png'],
      manifest: {
        name: 'Gestor de gastos',
        short_name: 'Gastos',
        description: 'Gastos personales del mes: vencimientos, pagos y resumen por categoría.',
        lang: 'es-AR',
        start_url: '/',
        scope: '/',
        display: 'standalone',
        background_color: '#ffffff',
        theme_color: '#2f5bd3',
        icons: [
          { src: 'pwa-192.png', sizes: '192x192', type: 'image/png' },
          { src: 'pwa-512.png', sizes: '512x512', type: 'image/png' },
          { src: 'pwa-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: {
        // Sin runtimeCaching: ninguna respuesta de /api se cachea. Las navegaciones a /api (p. ej.
        // la descarga del CSV) van siempre al servidor en lugar de caer en index.html.
        navigateFallbackDenylist: [/^\/api\//],
      },
    }),
  ],
  server: {
    proxy: {
      '/api': 'http://localhost:5033',
    },
  },
  build: {
    outDir: '../backend/src/GestorGastos.Api/wwwroot',
    emptyOutDir: true,
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./tests/setup.ts'],
    include: ['tests/**/*.test.{ts,tsx}'],
  },
})
