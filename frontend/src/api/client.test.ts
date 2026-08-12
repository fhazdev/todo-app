import { jest } from '@jest/globals'
import { ApiError, api, session } from './client'

type FetchArgs = [string, { headers?: Record<string, string> }?]

const fetchMock = jest.fn<(...args: FetchArgs) => Promise<Response>>()
globalThis.fetch = fetchMock as unknown as typeof fetch

function jsonResponse(status: number, body: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    headers: new Headers({ 'content-type': 'application/json' }),
    json: async () => body,
    text: async () => JSON.stringify(body),
  } as unknown as Response
}

function emptyResponse(status: number): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    headers: new Headers({ 'content-length': '0' }),
    json: async () => {
      throw new Error('no body')
    },
    text: async () => '',
  } as unknown as Response
}

const authResult = {
  user: { id: 'u1', email: 'maya@example.com', displayName: 'Maya Kern', initials: 'MK', avatarColor: '#c67139' },
  accessToken: 'access-2',
  accessTokenExpiresAt: '2099-01-01T00:00:00Z',
  refreshToken: 'refresh-2',
}

beforeEach(() => {
  fetchMock.mockReset()
  session.clear()
  localStorage.clear()
})

describe('session', () => {
  it('keeps the access token out of storage', () => {
    session.set({ ...authResult, accessToken: 'in-memory-only' })

    expect(session.accessToken).toBe('in-memory-only')
    expect(localStorage.getItem('sprout.refreshToken')).toBe('refresh-2')
    // The whole point: nothing in storage carries the access token.
    expect(JSON.stringify(localStorage)).not.toContain('in-memory-only')
  })

  it('clears both tokens', () => {
    session.set(authResult)
    session.clear()

    expect(session.accessToken).toBeNull()
    expect(session.refreshToken).toBeNull()
  })
})

describe('api', () => {
  it('sends the bearer token on an authenticated request', async () => {
    session.set(authResult)
    fetchMock.mockResolvedValueOnce(jsonResponse(200, [{ id: 'gro' }]))

    await api.get('/api/lists')

    expect(fetchMock.mock.calls[0][1]?.headers?.Authorization).toBe('Bearer access-2')
  })

  it('returns undefined for a 204 rather than throwing on an empty body', async () => {
    session.set(authResult)
    fetchMock.mockResolvedValueOnce(emptyResponse(204))

    await expect(api.put('/api/lists/gro/sort', { sort: 'MyOrder' })).resolves.toBeUndefined()
  })

  it('turns a problem document into an ApiError with its field map', async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse(400, {
        title: 'Check these fields',
        detail: 'One or more fields need attention.',
        errors: { email: ['That does not look like an email address.'] },
      }),
    )

    const error = await api.postAnonymous('/api/auth/register', {}).catch((caught) => caught)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).status).toBe(400)
    expect((error as ApiError).fieldError('email')).toBe('That does not look like an email address.')
  })

  it('still produces a usable error when the body is not JSON', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: false,
      status: 502,
      headers: new Headers(),
      json: async () => {
        throw new Error('not json')
      },
      text: async () => '<html>bad gateway</html>',
    } as unknown as Response)

    const error = (await api.get('/api/lists').catch((caught) => caught)) as ApiError

    expect(error.status).toBe(502)
    expect(error.message).toMatch(/could not reach the server/i)
  })

  it('refreshes once on a 401 and retries the original request', async () => {
    session.set({ ...authResult, accessToken: 'expired', refreshToken: 'refresh-1' })

    fetchMock
      .mockResolvedValueOnce(jsonResponse(401, { detail: 'expired' })) // original
      .mockResolvedValueOnce(jsonResponse(200, authResult)) // refresh
      .mockResolvedValueOnce(jsonResponse(200, [{ id: 'gro' }])) // retry

    await expect(api.get('/api/lists')).resolves.toEqual([{ id: 'gro' }])

    expect(fetchMock).toHaveBeenCalledTimes(3)
    expect(fetchMock.mock.calls[1][0]).toContain('/api/auth/refresh')
    // The retry carries the new token, not the expired one.
    expect(fetchMock.mock.calls[2][1]?.headers?.Authorization).toBe('Bearer access-2')
  })

  it('signs the caller out when the refresh token is spent too', async () => {
    session.set({ ...authResult, refreshToken: 'refresh-1' })

    fetchMock
      .mockResolvedValueOnce(jsonResponse(401, { detail: 'expired' }))
      .mockResolvedValueOnce(jsonResponse(401, { detail: 'that session has expired' }))

    await expect(api.get('/api/lists')).rejects.toBeInstanceOf(ApiError)
    expect(session.refreshToken).toBeNull()
  })

  it('spends the single-use refresh token only once when several requests race', async () => {
    // Starts on a stale access token, so all three requests hit a 401 together.
    session.set({ ...authResult, accessToken: 'expired', refreshToken: 'refresh-1' })

    fetchMock.mockImplementation((url, init) => {
      if (String(url).includes('/api/auth/refresh')) {
        return Promise.resolve(jsonResponse(200, authResult))
      }

      return Promise.resolve(
        init?.headers?.Authorization === 'Bearer access-2'
          ? jsonResponse(200, { ok: true })
          : jsonResponse(401, { detail: 'expired' }),
      )
    })

    await Promise.all([api.get('/api/lists'), api.get('/api/list-types'), api.get('/api/auth/me')])

    const refreshCalls = fetchMock.mock.calls.filter(([url]) => String(url).includes('/api/auth/refresh'))
    expect(refreshCalls).toHaveLength(1)
  })

  it('does not try to refresh an anonymous call', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse(401, { detail: 'wrong password' }))

    await expect(api.postAnonymous('/api/auth/login', {})).rejects.toBeInstanceOf(ApiError)
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})
