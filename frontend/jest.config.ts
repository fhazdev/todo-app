import type { Config } from 'jest'

/**
 * Jest over ts-jest in ESM mode, since the source is ESM and uses import.meta.
 * Vite is not involved in the test run, so the two things it normally provides,
 * the @/ alias and import.meta.env, are supplied here instead.
 */
const config: Config = {
  testEnvironment: 'jsdom',
  setupFilesAfterEnv: ['<rootDir>/src/test/setup.ts'],
  roots: ['<rootDir>/src'],
  testMatch: ['**/*.test.ts', '**/*.test.tsx'],

  extensionsToTreatAsEsm: ['.ts', '.tsx'],

  transform: {
    '^.+\\.tsx?$': [
      'ts-jest',
      {
        useESM: true,
        tsconfig: {
          jsx: 'react-jsx',
          module: 'ESNext',
          moduleResolution: 'bundler',
          target: 'ES2022',
          verbatimModuleSyntax: false,
          esModuleInterop: true,
          types: ['jest', 'node', '@testing-library/jest-dom'],
        },
      },
    ],
  },

  moduleNameMapper: {
    // Mirrors the Vite alias.
    '^@/(.*)\\.js$': '<rootDir>/src/$1',
    '^@/(.*)$': '<rootDir>/src/$1',
    '\\.css$': 'identity-obj-proxy',
  },

  clearMocks: true,
  restoreMocks: true,

  collectCoverageFrom: [
    'src/**/*.{ts,tsx}',
    '!src/main.tsx',
    '!src/**/*.d.ts',
    '!src/test/**',
  ],
}

export default config
