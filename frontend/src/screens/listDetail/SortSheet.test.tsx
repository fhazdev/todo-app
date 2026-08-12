import { jest } from '@jest/globals'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { SortSheet } from './SortSheet'
import { renderWithProviders } from '@/test/render'

function renderSheet(overrides: Partial<Parameters<typeof SortSheet>[0]> = {}) {
  const onChange = jest.fn()
  const onClose = jest.fn()

  renderWithProviders(
    <SortSheet
      open
      onClose={onClose}
      sort="Category"
      onChange={onChange}
      typeName="Grocery list"
      listTypeId="grocery"
      {...overrides}
    />,
  )

  return { onChange, onClose }
}

describe('SortSheet', () => {
  it('offers the four sorts from the design', () => {
    renderSheet()

    expect(screen.getAllByRole('radio').map((option) => option.textContent)).toEqual([
      expect.stringContaining('By category (custom)'),
      expect.stringContaining('My order'),
      expect.stringContaining('Due date'),
      expect.stringContaining('Alphabetical'),
    ])
  })

  it('names the type in the custom sort note', () => {
    renderSheet()

    expect(screen.getByText('Grocery list categories, your order')).toBeInTheDocument()
  })

  it('marks the current sort as selected', () => {
    renderSheet({ sort: 'DueDate' })

    expect(screen.getByRole('radio', { name: /Due date/ })).toHaveAttribute('aria-checked', 'true')
    expect(screen.getByRole('radio', { name: /My order/ })).toHaveAttribute('aria-checked', 'false')
  })

  it('reports a choice and closes', async () => {
    const { onChange, onClose } = renderSheet()

    await userEvent.click(screen.getByRole('radio', { name: /Alphabetical/ }))

    expect(onChange).toHaveBeenCalledWith('Alphabetical')
    expect(onClose).toHaveBeenCalled()
  })

  it('closes on Escape', async () => {
    const { onClose } = renderSheet()

    await userEvent.keyboard('{Escape}')

    expect(onClose).toHaveBeenCalled()
  })

  it('links to the typeategory screen', () => {
    renderSheet()

    expect(screen.getByRole('link', { name: /Edit this type's categories/ })).toHaveAttribute(
      'href',
      '/types/grocery',
    )
  })

  it('renders nothing when closed', () => {
    renderSheet({ open: false })

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})
