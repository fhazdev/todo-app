import { jest } from '@jest/globals'
import { buildRows, formatDue } from './rows'
import { groceryType, item, listDetail, plainType } from '@/test/fixtures'
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

    expect(rows[0]).toMatchObject({
      kind: 'item',
      category: groceryType.categories[0],
      showChip: true,
    })
  })

  it('drops the chip from rows sitting under a header that already names them', () => {
    const rows = buildRows(listDetail({ sort: 'Category' }), open)

    // Every grouped row keeps its category, for the checkbox colour, but hides the
    // chip: the header above it says "Fresh produce" already.
    const grouped = rows.filter((row) => row.kind === 'item')
    expect(grouped).toHaveLength(3)
    expect(grouped.every((row) => row.kind === 'item' && row.category !== null)).toBe(true)
    expect(grouped.every((row) => row.kind === 'item' && row.showChip === false)).toBe(true)
  })

  it('keeps the chip on the headerless uncategorised rows', () => {
    const loose = item({ id: 'loose', text: 'Batteries', categoryId: null })
    const rows = buildRows(listDetail({ sort: 'Category' }), [...open, loose])

    expect(rows.at(-1)).toMatchObject({ kind: 'item', category: null, showChip: true })
  })

  it('strips every trace of category on a plain list', () => {
    // The uncategorised rule: no headers, no chips, no dots.
    const plain = listDetail({ isPlain: true, type: plainType, sort: 'Category' })
    const rows = buildRows(plain, [item({ id: 'x', text: 'Piranesi', categoryId: null })])

    expect(rows).toEqual([
      {
        kind: 'item',
        key: 'x',
        item: expect.objectContaining({ text: 'Piranesi' }),
        category: null,
        showChip: true,
      },
    ])
  })

  it('still shows an item whose category has been deleted, without a header', () => {
    const orphan = item({ id: 'orphan', text: 'Left behind', categoryId: 'deleted-category' })
    const rows = buildRows(listDetail({ sort: 'Category' }), [...open, orphan])

    const last = rows.at(-1)
    expect(last).toMatchObject({ kind: 'item', category: null })
    expect(last && last.kind === 'item' && last.item.text).toBe('Left behind')
  })

  it('trails uncategorised items after the filed ones, with no header', () => {
    const loose = item({ id: 'loose', text: 'Batteries', categoryId: null })
    const rows = buildRows(listDetail({ sort: 'Category' }), [...open, loose])

    const last = rows.at(-1)
    expect(last).toMatchObject({ kind: 'item', category: null })
    expect(last && last.kind === 'item' && last.item.text).toBe('Batteries')

    // The filed items keep their headers; only the loose one goes without.
    expect(rows.filter((row) => row.kind === 'header')).toHaveLength(2)
  })

  it('draws no headers at all for a type that groups nothing', () => {
    const list = listDetail({ isPlain: true, type: plainType, sort: 'Category' })
    const rows = buildRows(list, [
      item({ id: 'a', text: 'Ring the vet', categoryId: null }),
      item({ id: 'b', text: 'Book the MOT', categoryId: null }),
    ])

    expect(rows.every((row) => row.kind === 'item' && row.category === null)).toBe(true)
    expect(rows).toHaveLength(2)
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
