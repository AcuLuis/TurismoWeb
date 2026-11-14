import js from '@eslint/js'
import globals from 'globals'
import pluginVue from 'eslint-plugin-vue'
import prettierSkipFormatting from 'eslint-config-prettier'

export default [
  {
    ignores: ['dist/**', '.quasar/**', 'node_modules/**', 'quasar.config.*.temporary.compiled*'],
  },

  js.configs.recommended,
  ...pluginVue.configs['flat/recommended'],

  {
    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',
      globals: {
        ...globals.browser,
        ...globals.node,
        process: 'readonly',
      },
    },
    rules: {
      'prefer-promise-reject-errors': 'off',
      'no-debugger': process.env.NODE_ENV === 'production' ? 'error' : 'off',
      'no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
      'vue/multi-word-component-names': 'off',
    },
  },

  {
    files: ['**/*.test.js'],
    languageOptions: { globals: { ...globals.vitest } },
  },

  prettierSkipFormatting,
]
