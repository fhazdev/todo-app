import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'

/**
 * ESLint 9 flat config.
 *
 * TypeScript already covers types and undefined identifiers, so this is here for
 * the things the compiler does not see: hook dependency arrays, fast-refresh
 * boundaries, and floating promises. The type-aware rules need a TS program, which
 * is why the source globs are tied to the tsconfigs rather than linted loose.
 */
export default tseslint.config(
  { ignores: ['dist/**', 'coverage/**', 'node_modules/**'] },

  // ── Application and test sources ────────────────────────────────────────────
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      ...tseslint.configs.recommendedTypeChecked,
    ],
    languageOptions: {
      globals: { ...globals.browser },
      parserOptions: {
        // Listed explicitly rather than via projectService, which only discovers
        // projects referenced from tsconfig.json. tsconfig.test.json is deliberately
        // not referenced there, so that a type error in a test cannot break the
        // production build; naming it here lets lint see test files anyway.
        project: ['./tsconfig.app.json', './tsconfig.test.json', './tsconfig.node.json'],
        tsconfigRootDir: import.meta.dirname,
      },
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended[0].rules,
      ...reactRefresh.configs.vite.rules,

      // A warning, not an error. It currently flags one real thing: AuthContext.tsx
      // exports both the context and its provider, so editing it full-reloads
      // instead of hot-swapping. Worth knowing, not worth failing a build over.
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],

      // The codebase marks deliberately unawaited promises with `void`, which is
      // exactly what this rule is for. Kept as an error so the habit is enforced
      // rather than merely conventional.
      '@typescript-eslint/no-floating-promises': 'error',

      // Underscore-prefixed arguments are the established signal for "required by
      // the signature, unused on purpose", as in TanStack Query's onError handlers.
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_', caughtErrors: 'none' },
      ],
    },
  },

  // ── Tests ───────────────────────────────────────────────────────────────────
  {
    files: ['**/*.test.{ts,tsx}', 'src/test/**/*.{ts,tsx}'],
    languageOptions: { globals: { ...globals.node } },
    rules: {
      // A stubbed Response has to implement `json()` and `text()` as async to match
      // the interface it stands in for, with nothing to await inside. The rule is
      // reading a faithful test double as a mistake.
      '@typescript-eslint/require-await': 'off',

      // Jest's own typings hand back `any` from expect.objectContaining and from
      // mock callbacks, so these fire on the matcher rather than on our code. The
      // same rules stay on for the app, where an `any` really is a hole.
      '@typescript-eslint/no-unsafe-assignment': 'off',
      '@typescript-eslint/no-unsafe-return': 'off',
      '@typescript-eslint/no-unsafe-argument': 'off',
      '@typescript-eslint/no-unsafe-member-access': 'off',
    },
  },

  // ── Config files that run in Node ───────────────────────────────────────────
  {
    files: ['*.config.{ts,js}', 'jest.config.ts'],
    languageOptions: { globals: { ...globals.node } },
  },
)
