import '@testing-library/jest-dom'
// Under ESM, Jest does not inject `jest` as a global; it has to be imported.
import { jest } from '@jest/globals'
import { TextDecoder, TextEncoder } from 'node:util'

// jsdom ships without the text encoding globals, which react-router reaches for
// on import. Every browser has had them for years, so Node's are a fair stand-in.
if (typeof globalThis.TextEncoder === 'undefined') {
  Object.assign(globalThis, { TextEncoder, TextDecoder })
}

// jsdom implements neither of these, and the sheet and list components both use
// them. Stubbing here keeps every test file from having to.
if (!window.matchMedia) {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
      dispatchEvent: jest.fn(),
    }),
  })
}

if (!Element.prototype.scrollIntoView) {
  Element.prototype.scrollIntoView = jest.fn()
}

// Nothing is needed for import.meta.env here: src/lib/env.ts reads it defensively
// and falls back to the local API, precisely so tests do not have to stub Vite.
