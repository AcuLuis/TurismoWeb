// Configuración de Quasar (app-vite v3)
// https://v2.quasar.dev/quasar-cli-vite/quasar-config-file
import { defineConfig } from '#q-app'

export default defineConfig((/* ctx */) => {
  return {
    boot: ['axios'],

    css: ['app.scss'],

    extras: ['roboto-font', 'material-icons', 'material-icons-outlined'],

    build: {
      target: {
        browser: ['es2022', 'firefox115', 'chrome115', 'safari14'],
        node: 'node20',
      },

      // Rutas limpias (/actividades/5). En producción el servidor debe redirigir rutas desconocidas a index.html.
      vueRouterMode: 'history',

      // Solo las variables con prefijo TG_ (de archivos .env* o del entorno) llegan al navegador,
      // accesibles como import.meta.env.TG_*. Ver .env.example.
      env: {
        clientPrefix: 'TG_',
      },

      vitePlugins: [
        [
          'vite-plugin-checker',
          {
            eslint: {
              lintCommand: 'eslint -c ./eslint.config.js "./src*/**/*.{js,mjs,cjs,vue}"',
              useFlatConfig: true,
            },
          },
          { server: false },
        ],
      ],
    },

    devServer: {
      port: 9000,
      open: true,
    },

    framework: {
      config: {},
      lang: 'es',
      iconSet: 'material-icons',
      plugins: [],
    },

    animations: [],
  }
})
