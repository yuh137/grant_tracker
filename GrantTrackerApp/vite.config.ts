/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const apiUrl = process.env.VITE_API_URL ?? 'http://localhost:5180'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/grants': apiUrl,
      '/tasks': apiUrl,
      '/reports': apiUrl,
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
})
