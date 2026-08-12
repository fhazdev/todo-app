/**
 * Build-time configuration, read once and in one place.
 *
 * Vite replaces import.meta.env at build time, but Jest runs the modules through
 * ts-jest with no such replacement, so import.meta.env is simply absent there.
 * Reading it defensively here means no test has to stub it and no other module
 * has to think about it.
 */
const meta = import.meta as ImportMeta & { env?: Record<string, string | undefined> }

export const env = {
  /** Where the Sprout API lives. Overridden per environment by VITE_API_BASE_URL. */
  apiBaseUrl: meta.env?.VITE_API_BASE_URL ?? 'http://localhost:5080',

  /** Empty when Google sign-in is not configured, which hides the button entirely. */
  googleClientId: meta.env?.VITE_GOOGLE_CLIENT_ID ?? '',
} as const
