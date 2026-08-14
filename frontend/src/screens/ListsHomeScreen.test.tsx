import { jest } from '@jest/globals'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ListsHomeScreen } from './ListsHomeScreen'
import { renderWithProviders } from '@/test/render'
import { listSummary } from '@/test/fixtures'
import { AuthContext, type AuthContextValue } from '@/auth/AuthContext'
import type { ReactElement } from 'react'

const auth: AuthContextValue = {
  user: { id: 'u1', email: 'maya@example.com', displayName: 'Maya Kern', initials: 'MK', avatarColor: '#c67139' },
  isRestoring: false,
  signIn: jest.fn(async () => {}),
  register: jest.fn(async () => {}),
  signInWithGoogle: jest.fn(async () => {}),
  signOut: jest.fn(async () => {}),
}

function withAuth(ui: ReactElement) {
  return <AuthContext.Provider value={auth}>{ui}</AuthContext.Provider>
}

const fetchMock = jest.fn<() => Promise<Response>>()
globalThis.fetch = fetchMock as unknown as typeof fetch

function respondWith(body: unknown) {
  fetchMock.mockResolvedValue({
    ok: true,
    status: 200,
    headers: new Headers({ 'content-type': 'application/json' }),
    json: async () => body,
    text: async () => JSON.stringify(body),
  } as unknown as Response)
}

describe('ListsHomeScreen', () => {
  it('shows the account email under the title', async () => {
    respondWith([])
    renderWithProviders(withAuth(<ListsHomeScreen />))

    expect(screen.getByRole('heading', { name: 'My lists' })).toBeInTheDocument()
    expect(screen.getByText('maya@example.com')).toBeInTheDocument()
  })

  it('renders a card per list with its type chip and meta line', async () => {
    respondWith([listSummary()])
    renderWithProviders(withAuth(<ListsHomeScreen />))

    await waitFor(() => expect(screen.getByText('Groceries')).toBeInTheDocument())

    expect(screen.getByText('Grocery list')).toBeInTheDocument()
    expect(screen.getByText('5 left · shared with 2')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Groceries/ })).toHaveAttribute('href', '/lists/gro')
  })

  it('says "just you" for a list nobody else is on', async () => {
    respondWith([listSummary({ sharedWithCount: 0, members: [], openCount: 3 })])
    renderWithProviders(withAuth(<ListsHomeScreen />))

    await waitFor(() => expect(screen.getByText('3 left · just you')).toBeInTheDocument())
  })

  it('offers an empty state rather than a blank screen', async () => {
    respondWith([])
    renderWithProviders(withAuth(<ListsHomeScreen />))

    await waitFor(() => expect(screen.getByText('No lists yet')).toBeInTheDocument())
  })

  it('signs out from the header', async () => {
    respondWith([])
    renderWithProviders(withAuth(<ListsHomeScreen />))

    await userEvent.click(screen.getByRole('button', { name: 'Sign out' }))

    // Clearing the session is all this screen does; RequireAuth is what redirects.
    expect(auth.signOut).toHaveBeenCalledTimes(1)
  })

  it('surfaces a failure with a retry', async () => {
    fetchMock.mockResolvedValue({
      ok: false,
      status: 500,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ detail: 'Sprout could not complete that. Try again.' }),
      text: async () => '{}',
    } as unknown as Response)

    renderWithProviders(withAuth(<ListsHomeScreen />))

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
