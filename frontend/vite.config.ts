import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'node:path'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: {
    // 0.0.0.0 so the dev server is reachable from outside the container.
    host: true,
    port: 5173,
    // Polling: file events from a Windows host do not reach a Linux container.
    watch: { usePolling: true },
  },
  preview: { host: true, port: 4173 },
})
