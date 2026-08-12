import type { Category, TodoItem, TodoListDetail } from '@/api/types'

export interface HeaderRow {
  kind: 'header'
  key: string
  category: Category
  count: number
}

export interface ItemRow {
  kind: 'item'
  key: string
  item: TodoItem
  /** Null on a plain list, where no category chrome is drawn at all. */
  category: Category | null
}

export type Row = HeaderRow | ItemRow

/**
 * Turns the server's already-sorted item array into the rows the list renders.
 *
 * The server decides the order; this only decides where category headers go, which
 * is a purely visual concern:
 *
 * - headers appear only when the sort is By category and the list is not plain
 * - a category with nothing in it under the current filter renders no header
 * - on a plain list the chips, dots and headers all disappear, and the checkbox
 *   falls back to the accent colour
 */
export function buildRows(list: TodoListDetail, items: TodoItem[]): Row[] {
  const categories = new Map(list.type.categories.map((category) => [category.id, category]))

  if (list.isPlain || list.sort !== 'Category') {
    return items.map((item) => ({
      kind: 'item',
      key: item.id,
      item,
      category: list.isPlain ? null : (categories.get(item.categoryId) ?? null),
    }))
  }

  const rows: Row[] = []

  for (const category of list.type.categories) {
    const group = items.filter((item) => item.categoryId === category.id)
    if (group.length === 0) continue

    rows.push({ kind: 'header', key: `h-${category.id}`, category, count: group.length })

    for (const item of group) {
      rows.push({ kind: 'item', key: item.id, item, category })
    }
  }

  // Items whose category was deleted out from under them still have to show up,
  // and they do so without a header rather than vanishing.
  for (const item of items) {
    if (!categories.has(item.categoryId)) {
      rows.push({ kind: 'item', key: item.id, item, category: null })
    }
  }

  return rows
}

/** The accent used when a list has no category chrome. */
export const PLAIN_ACCENT = '#c67139'

/** "Sat", "12 Aug": short enough for the 11px meta line. */
export function formatDue(dueOn: string | null): string {
  if (!dueOn) return ''

  const date = new Date(`${dueOn}T00:00:00`)
  if (Number.isNaN(date.getTime())) return ''

  const today = new Date()
  today.setHours(0, 0, 0, 0)

  const days = Math.round((date.getTime() - today.getTime()) / 86_400_000)

  if (days === 0) return 'Today'
  if (days === 1) return 'Tomorrow'
  if (days > 1 && days < 7) return date.toLocaleDateString(undefined, { weekday: 'short' })

  return date.toLocaleDateString(undefined, { day: 'numeric', month: 'short' })
}
