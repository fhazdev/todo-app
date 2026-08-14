import { jest } from '@jest/globals'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ItemRow } from './ItemRow'
import { groceryCategories, item } from '@/test/fixtures'

const produce = groceryCategories[0]

function renderRow(props: Partial<Parameters<typeof ItemRow>[0]> = {}) {
  const onToggle = jest.fn()

  render(
    <ul>
      <ItemRow
        item={item({ id: 'i1', text: 'Bananas', categoryId: 'produce' })}
        category={produce}
        onToggle={onToggle}
        {...props}
      />
    </ul>,
  )

  return { onToggle }
}

describe('ItemRow', () => {
  it('exposes the circle as a checkbox labelled with the item', () => {
    renderRow()

    const checkbox = screen.getByRole('checkbox', { name: 'Bananas' })
    expect(checkbox).toHaveAttribute('aria-checked', 'false')
  })

  it('toggles when the circle is tapped', async () => {
    const { onToggle } = renderRow()

    await userEvent.click(screen.getByRole('checkbox', { name: 'Bananas' }))

    expect(onToggle).toHaveBeenCalledTimes(1)
  })

  it('gives the tap target at least the 44px floor the handoff sets', () => {
    renderRow()

    // The visible circle is 26px; the button around it is the real hit area.
    expect(screen.getByRole('checkbox', { name: 'Bananas' }).className).toMatch(/h-11 w-11/)
  })

  it('shows the category chip in the category colours', () => {
    renderRow()

    const chip = screen.getByText('Fresh produce')
    expect(chip).toHaveStyle({ background: produce.tint, color: produce.deep })
  })

  it('strikes through a completed item', () => {
    renderRow({ item: item({ id: 'i1', text: 'Halloumi', categoryId: 'produce', isCompleted: true }) })

    expect(screen.getByRole('checkbox', { name: 'Halloumi' })).toHaveAttribute('aria-checked', 'true')
    expect(screen.getByText('Halloumi').className).toMatch(/line-through/)
  })

  it('draws no chip and no dot when the list is plain', () => {
    renderRow({ category: null })

    expect(screen.queryByText('Fresh produce')).not.toBeInTheDocument()
    expect(screen.getByText('Bananas')).toBeInTheDocument()
  })

  it('hides the chip under a header while keeping the category checkbox colour', () => {
    renderRow({ showChip: false })

    // The header above already says "Fresh produce"; repeating it on the row is noise.
    expect(screen.queryByText('Fresh produce')).not.toBeInTheDocument()

    // The circle still carries the category colour, so the row stays tied to its group.
    const circle = screen.getByRole('checkbox', { name: 'Bananas' }).firstElementChild
    expect(circle).toHaveStyle({ border: `2.75px solid ${produce.color}` })
  })

  it('shows the due date beside the chip', () => {
    renderRow({
      item: item({ id: 'i1', text: 'Sourdough', categoryId: 'produce', dueOn: '2099-01-15' }),
    })

    expect(screen.getByText(/15/)).toBeInTheDocument()
  })
})
