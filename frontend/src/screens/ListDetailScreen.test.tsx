import { jest } from '@jest/globals'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Route, Routes } from 'react-router-dom'
import { ListDetailScreen } from './ListDetailScreen'
import { renderWithProviders } from '@/test/render'
import { listDetail } from '@/test/fixtures'
import type { TodoListDetail } from '@/api/types'

const fetchMock = jest.fn<(url: string, init?: RequestInit) => Promise<Response>>()
globalThis.fetch = fetchMock as unknown as typeof fetch

function json(body: unknown): Response {
  return {
    ok: true,
    status: 200,
    headers: new Headers({ 'content-type': 'application/json' }),
    json: async () => body,
    text: async () => JSON.stringify(body),
  } as unknown as Response
}

function noContent(): Response {
  return {
    ok: true,
    status: 204,
    headers: new Headers(),
    json: async () => undefined,
    text: async () => '',
  } as unknown as Response
}

/** GET returns the list; DELETE succeeds. */
function serve(detail: TodoListDetail) {
  fetchMock.mockImplementation(async (_url, init) =>
    init?.method === 'DELETE' ? noContent() : json(detail),
  )
}

function renderScreen() {
  return renderWithProviders(
    <Routes>
      <Route path="/lists" element={<div>Lists home</div>} />
      <Route path="/lists/:listId" element={<ListDetailScreen />} />
    </Routes>,
    { route: '/lists/gro' },
  )
}

function deleteCalls() {
  return fetchMock.mock.calls.filter(([, init]) => init?.method === 'DELETE')
}

describe('ListDetailScreen deletion', () => {
  beforeEach(() => fetchMock.mockReset())

  it('offers the owner a way to delete the list', async () => {
    serve(listDetail())
    renderScreen()

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Groceries' })).toBeInTheDocument())
    expect(screen.getByRole('button', { name: 'Delete Groceries' })).toBeInTheDocument()
  })

  it('hides deletion from an editor, as the server would refuse it anyway', async () => {
    serve(listDetail({ myRole: 'Editor' }))
    renderScreen()

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Groceries' })).toBeInTheDocument())
    expect(screen.queryByRole('button', { name: 'Delete Groceries' })).not.toBeInTheDocument()
  })

  it('confirms before deleting rather than acting on the first tap', async () => {
    serve(listDetail())
    renderScreen()

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Groceries' })).toBeInTheDocument())
    await userEvent.click(screen.getByRole('button', { name: 'Delete Groceries' }))

    expect(screen.getByRole('dialog', { name: 'Delete Groceries?' })).toBeInTheDocument()
    expect(deleteCalls()).toHaveLength(0)
  })

  it('spells out what the delete takes with it', async () => {
    serve(listDetail())
    renderScreen()

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Groceries' })).toBeInTheDocument())
    await userEvent.click(screen.getByRole('button', { name: 'Delete Groceries' }))

    // Three items, and the two people who are not you: one active editor and one
    // pending invitation, which the delete revokes as well.
    expect(screen.getByText(/3 items and 2 people it is shared with/)).toBeInTheDocument()
    expect(screen.getByText(/cannot be undone/)).toBeInTheDocument()
  })

  it('backs out without sending anything', async () => {
    serve(listDetail())
    renderScreen()

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Groceries' })).toBeInTheDocument())
    await userEvent.click(screen.getByRole('button', { name: 'Delete Groceries' }))
    await userEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(deleteCalls()).toHaveLength(0)
  })

  it('deletes on confirmation and returns to the lists', async () => {
    serve(listDetail())
    renderScreen()

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Groceries' })).toBeInTheDocument())
    await userEvent.click(screen.getByRole('button', { name: 'Delete Groceries' }))
    await userEvent.click(screen.getByRole('button', { name: 'Delete list' }))

    await waitFor(() => expect(screen.getByText('Lists home')).toBeInTheDocument())

    expect(deleteCalls()).toHaveLength(1)
    expect(deleteCalls()[0][0]).toContain('/api/lists/gro')
  })

  it('stays put and explains itself when the delete fails', async () => {
    fetchMock.mockImplementation(async (_url, init) => {
      if (init?.method !== 'DELETE') return json(listDetail())

      return {
        ok: false,
        status: 500,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ detail: 'Sprout could not complete that. Try again.' }),
        text: async () => JSON.stringify({ detail: 'Sprout could not complete that. Try again.' }),
      } as unknown as Response
    })

    renderScreen()

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Groceries' })).toBeInTheDocument())
    await userEvent.click(screen.getByRole('button', { name: 'Delete Groceries' }))
    await userEvent.click(screen.getByRole('button', { name: 'Delete list' }))

    // Navigating away on a failed delete would look like it had worked.
    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.queryByText('Lists home')).not.toBeInTheDocument()
  })
})
