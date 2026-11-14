import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vitest/config'

export default defineConfig({
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  test: {
    environment: 'node',
    include: ['src/**/*.test.js'],
    // En Windows, si la terminal entrega la ruta con la unidad en minúscula ("c:\...", como la de VS Code),
    // los pools "threads"/"forks" cargan dos copias de Vitest y fallan. "vmThreads" no tiene ese problema.
    pool: 'vmThreads',
    // Zona horaria de Perú: reproduce el escenario del error de fechas que se corrigió
    env: { TZ: 'America/Lima' },
  },
})
