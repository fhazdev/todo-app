import { env } from '@/lib/env'
import type { AuthResult, ProblemDocument } from './types'

const BASE_URL = env.apiBaseUrl

/**
 * An error carrying everything a form needs to render itself: the message to show
 * at the top, and the per-field messages to place next to inputs.
 */
export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }

  /** The first message for a field, for rendering under its input. */
  fieldError(field: string): string | undefined {
    return this.fieldErrors[field]?.[0]
  }

  get isUnauthorised(): boolean {
    return this.status === 401
  }
}

/**
 * Where the session lives.
 *
 * The access token is held in memory only, so a script that reads localStorage
 * cannot lift it. The refresh token has to survive a reload for "stay signed in"
 * to mean anything, so that one is persisted; it is single-use and rotates on
 * every exchange, which is what limits the damage if it does leak.
 */
let accessToken: string | null = null

const REFRESH_TOKEN_KEY = 'sprout.refreshToken'

export const session = {
  get accessToken(): string | null {
    return accessToken
  },

  get refreshToken(): string | null {
    try {
      return localStorage.getItem(REFRESH_TOKEN_KEY)
    } catch {
      // Private browsing modes can throw on storage access.
      return null
    }
  },

  set(result: AuthResult): void {
    accessToken = result.accessToken
    try {
      localStorage.setItem(REFRESH_TOKEN_KEY, result.refreshToken)
    } catch {
      // Nothing to do: the session simply will not survive a reload.
    }
  },

  clear(): void {
    accessToken = null
    try {
      localStorage.removeItem(REFRESH_TOKEN_KEY)
    } catch {
      // Ignored, as above.
    }
  },
}

/**
 * In-flight refresh, shared by every request that hits a 401 at once, so a page
 * with several queries does not spend its single-use refresh token more than once.
 */
let refreshInFlight: Promise<boolean> | null = null

async function refreshSession(): Promise<boolean> {
  const token = session.refreshToken
  if (!token) return false

  refreshInFlight ??= (async () => {
    try {
      const response = await fetch(`${BASE_URL}/api/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: token }),
      })

      if (!response.ok) {
        session.clear()
        return false
      }

      session.set((await response.json()) as AuthResult)
      return true
    } catch {
      session.clear()
      return false
    } finally {
      refreshInFlight = null
    }
  })()

  return refreshInFlight
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  /** Set for the auth endpoints, which must not trigger a refresh of their own. */
  skipAuth?: boolean
  signal?: AbortSignal
}

async function send(path: string, options: RequestOptions): Promise<Response> {
  const headers: Record<string, string> = {}

  if (options.body !== undefined) headers['Content-Type'] = 'application/json'
  if (!options.skipAuth && accessToken) headers.Authorization = `Bearer ${accessToken}`

  return fetch(`${BASE_URL}${path}`, {
    method: options.method ?? 'GET',
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    signal: options.signal,
  })
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDocument = {}

  try {
    problem = (await response.json()) as ProblemDocument
  } catch {
    // A non-JSON error body (a proxy's HTML page, say) still gets a usable message.
  }

  const message =
    problem.detail ??
    problem.title ??
    (response.status >= 500
      ? 'Sprout could not reach the server. Try again.'
      : 'That did not work.')

  return new ApiError(response.status, message, problem.errors ?? {})
}

/**
 * Makes a request, and on a 401 refreshes the session once and retries. A second
 * 401 means the refresh token is spent too, and the caller is signed out.
 */
async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  let response = await send(path, options)

  if (response.status === 401 && !options.skipAuth) {
    response = (await refreshSession()) ? await send(path, options) : response
  }

  if (!response.ok) throw await toApiError(response)

  // 204, and any other empty body, deserialises to undefined rather than throwing.
  if (response.status === 204 || response.headers.get('content-length') === '0') {
    return undefined as T
  }

  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

export const api = {
  get: <T>(path: string, signal?: AbortSignal) => request<T>(path, { signal }),
  post: <T>(path: string, body?: unknown) => request<T>(path, { method: 'POST', body }),
  put: <T>(path: string, body?: unknown) => request<T>(path, { method: 'PUT', body }),
  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),

  /** Auth calls bypass the refresh-and-retry loop, since they are the loop. */
  postAnonymous: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body, skipAuth: true }),

  refreshSession,
}
