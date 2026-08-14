import { jest } from '@jest/globals'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Route, Routes } from 'react-router-dom'
import { TypeCategoriesScreen } from './TypeCategoriesScreen'
import { renderWithProviders } from '@/test/render'
import { groceryType } from '@/test/fixtures'

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

/** The screen reads its id from the route, so it needs a real matched Route. */
function renderScreen() {
  return renderWithProviders(
    <Routes>
      <Route path="/types/:listTypeId" element={<TypeCategoriesScreen />} />
    </Routes>,
    { route: '/types/grocery' },
  )
}

function putCalls() {
  return fetchMock.mock.calls.filter(([, init]) => init?.method === 'PUT')
}

describe('TypeCategoriesScreen', () => {
  beforeEach(() => {
    fetchMock.mockReset()
    fetchMock.mockResolvedValue(json(groceryType))
  })

  it('lists the categories in their custom order', async () => {
    renderScreen()

    await waitFor(() => expect(screen.getByText('Fresh produce')).toBeInTheDocument())
    expect(screen.getByText('Bread & bakery')).toBeInTheDocument()
    expect(screen.getByText('Dairy')).toBeInTheDocument()
  })

  it('renames a category from the row it is on', async () => {
    renderScreen()
    await waitFor(() => expect(screen.getByText('Dairy')).toBeInTheDocument())

    await userEvent.click(screen.getByRole('button', { name: 'Rename Dairy' }))

    const field = screen.getByRole('textbox', { name: 'Rename Dairy' })
    expect(field).toHaveValue('Dairy')

    await userEvent.clear(field)
    await userEvent.type(field, 'Dairy & eggs')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(putCalls()).toHaveLength(1))

    const [url, init] = putCalls()[0]
    expect(url).toContain('/api/list-types/grocery/categories/dairy')
    // The body is always a JSON string here; BodyInit is wider than that, hence
    // the assertion rather than String(), which would stringify an object to
    // "[object Object]" and pass a nonsense value to JSON.parse.
    expect(JSON.parse(init?.body as string)).toEqual({ name: 'Dairy & eggs' })
  })

  it('commits on Enter', async () => {
    renderScreen()
    await waitFor(() => expect(screen.getByText('Dairy')).toBeInTheDocument())

    await userEvent.click(screen.getByRole('button', { name: 'Rename Dairy' }))
    await userEvent.clear(screen.getByRole('textbox', { name: 'Rename Dairy' }))
    await userEvent.type(screen.getByRole('textbox', { name: 'Rename Dairy' }), 'Cheese{Enter}')

    await waitFor(() => expect(putCalls()).toHaveLength(1))
  })

  it('abandons the edit on Escape without sending anything', async () => {
    renderScreen()
    await waitFor(() => expect(screen.getByText('Dairy')).toBeInTheDocument())

    await userEvent.click(screen.getByRole('button', { name: 'Rename Dairy' }))
    await userEvent.type(screen.getByRole('textbox', { name: 'Rename Dairy' }), 'nonsense')
    await userEvent.keyboard('{Escape}')

    await waitFor(() => expect(screen.getByText('Dairy')).toBeInTheDocument())
    expect(putCalls()).toHaveLength(0)
  })

  it('does not send a request when the name comes back unchanged', async () => {
    renderScreen()
    await waitFor(() => expect(screen.getByText('Dairy')).toBeInTheDocument())

    await userEvent.click(screen.getByRole('button', { name: 'Rename Dairy' }))
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(screen.getByText('Dairy')).toBeInTheDocument())
    expect(putCalls()).toHaveLength(0)
  })
})
