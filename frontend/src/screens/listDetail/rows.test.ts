import { jest } from '@jest/globals'
import { buildRows, formatDue } from './rows'
import { catchAllType, groceryType, item, listDetail } from '@/test/fixtures'
import type { TodoItem } from '@/api/types'

/**
 * The row builder decides where category headers go. The server owns the item
 * order; these tests cover only the chrome rules from the handoff.
 */
describe('buildRows', () => {
  const open: TodoItem[] = [
    item({ id: 'i1', text: 'Bananas', categoryId: 'produce' }),
    item({ id: 'i2', text: 'Rocket', categoryId: 'produce' }),
    item({ id: 'i3', text: 'Sourdough', categoryId: 'bakery' }),
  ]

  it('inserts a header before each non-empty group, in the type order', () => {
    const rows = buildRows(listDetail({ sort: 'Category' }), open)

    expect(rows.map((row) => (row.kind === 'header' ? `#${row.category.name}` : row.item.text))).toEqual([
      '#Fresh produce',
      'Bananas',
      'Rocket',
      '#Bread & bakery',
      'Sourdough',
    ])
  })

  it('renders no header for a category with nothing in it', () => {
    // Dairy is on the type but has no open items here.
    const rows = buildRows(listDetail({ sort: 'Category' }), open)

    expect(rows.some((row) => row.kind === 'header' && row.category.name === 'Dairy')).toBe(false)
  })

  it('counts the items in each group', () => {
    const rows = buildRows(listDetail({ sort: 'Category' }), open)
    const produce = rows.find((row) => row.kind === 'header')

    expect(produce).toMatchObject({ kind: 'header', count: 2 })
  })

  it('drops headers entirely for any sort other than By category', () => {
    const rows = buildRows(listDetail({ sort: 'Alphabetical' }), open)

    expect(rows.every((row) => row.kind === 'item')).toBe(true)
    expect(rows).toHaveLength(3)
  })

  it('keeps category chips on the rows when the sort is not By category', () => {
    const rows = buildRows(listDetail({ sort: 'MyOrder' }), open)

    expect(rows[0]).toMatchObject({ kind: 'item', category: groceryType.categories[0] })
  })

  it('strips every trace of category on a plain list', () => {
    // The uncategorised rule: no headers, no chips, no dots.
    const plain = listDetail({ isPlain: true, type: catchAllType, sort: 'Category' })
    const rows = buildRows(plain, [item({ id: 'x', text: 'Piranesi', categoryId: 'uncategorised' })])

    expect(rows).toEqual([
      { kind: 'item', key: 'x', item: expect.objectContaining({ text: 'Piranesi' }), category: null },
    ])
  })

  it('still shows an item whose category has been deleted, without a header', () => {
    const orphan = item({ id: 'orphan', text: 'Left behind', categoryId: 'deleted-category' })
    const rows = buildRows(listDetail({ sort: 'Category' }), [...open, orphan])

    const last = rows.at(-1)
    expect(last).toMatchObject({ kind: 'item', category: null })
    expect(last && last.kind === 'item' && last.item.text).toBe('Left behind')
  })
})

describe('formatDue', () => {
  beforeEach(() => {
    // Pin "now" so the relative labels are deterministic. Wednesday 12 August 2026.
    jest.useFakeTimers().setSystemTime(new Date('2026-08-12T09:00:00Z'))
  })

  afterEach(() => jest.useRealTimers())

  it('is empty for an item with no due date', () => {
    expect(formatDue(null)).toBe('')
  })

  it('names today and tomorrow rather than dating them', () => {
    expect(formatDue('2026-08-12')).toBe('Today')
    expect(formatDue('2026-08-13')).toBe('Tomorrow')
  })

  it('uses a weekday inside the coming week', () => {
    // 15 August 2026 is a Saturday.
    expect(formatDue('2026-08-15')).toBe('Sat')
  })

  it('falls back to a short date further out', () => {
    expect(formatDue('2026-09-30')).toMatch(/30/)
  })

  it('ignores an unparseable date rather than rendering NaN', () => {
    expect(formatDue('not-a-date')).toBe('')
  })
})
