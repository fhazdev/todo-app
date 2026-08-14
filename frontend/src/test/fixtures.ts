import type { Category, ListType, Member, TodoItem, TodoListDetail, TodoListSummary } from '@/api/types'

/** The Grocery list type from the handoff, colours and all. */
export const groceryCategories: Category[] = [
  { id: 'produce', name: 'Fresh produce', position: 0, paletteIndex: 0, color: '#c67139', tint: '#ffe1d0', deep: '#8c491a' },
  { id: 'bakery', name: 'Bread & bakery', position: 1, paletteIndex: 1, color: '#7a8a5e', tint: '#e1eecc', deep: '#56633f' },
  { id: 'dairy', name: 'Dairy', position: 2, paletteIndex: 2, color: '#b2622d', tint: '#fff2eb', deep: '#643312' },
]

export const groceryType: ListType = {
  id: 'grocery',
  name: 'Grocery list',
  blurb: 'Aisles you shop in',
  categories: groceryCategories,
  listCount: 1,
  isDefault: false,
}

/** A type that groups nothing, as the Default list does. Its lists are plain. */
export const plainType: ListType = {
  id: 'reading',
  name: 'Reading list',
  blurb: null,
  categories: [],
  listCount: 1,
  isDefault: false,
}

export const members: Member[] = [
  { id: 'm1', userId: 'u1', displayName: 'Maya Kern', email: 'maya@example.com', initials: 'MK', avatarColor: '#c67139', role: 'Owner', status: 'Active', isYou: true },
  { id: 'm2', userId: 'u2', displayName: 'Nina Boye', email: 'nina@example.com', initials: 'NB', avatarColor: '#7a8a5e', role: 'Editor', status: 'Active', isYou: false },
  { id: 'm3', userId: null, displayName: 'sam.oyelaran@example.com', email: 'sam.oyelaran@example.com', initials: 'SA', avatarColor: '#82796a', role: 'Editor', status: 'Invited', isYou: false },
]

export function item(overrides: Partial<TodoItem> & Pick<TodoItem, 'id' | 'text' | 'categoryId'>): TodoItem {
  return { dueOn: null, isCompleted: false, position: 0, quantity: 1, ...overrides }
}

/**
 * A list detail as the API returns it: items already sorted, open ones first.
 */
export function listDetail(overrides: Partial<TodoListDetail> = {}): TodoListDetail {
  return {
    id: 'gro',
    name: 'Groceries',
    type: groceryType,
    sort: 'Category',
    showCompleted: false,
    isPlain: false,
    myRole: 'Owner',
    items: [
      item({ id: 'i1', text: 'Bananas', categoryId: 'produce', position: 1 }),
      item({ id: 'i2', text: 'Sourdough', categoryId: 'bakery', position: 0, dueOn: '2026-08-15' }),
      item({ id: 'i3', text: 'Halloumi', categoryId: 'dairy', position: 2, isCompleted: true }),
    ],
    members,
    ...overrides,
  }
}

export function listSummary(overrides: Partial<TodoListSummary> = {}): TodoListSummary {
  return {
    id: 'gro',
    name: 'Groceries',
    listTypeId: 'grocery',
    typeName: 'Grocery list',
    typeColor: '#c67139',
    typeTint: '#ffe1d0',
    typeDeep: '#8c491a',
    openCount: 5,
    sharedWithCount: 2,
    members: members.slice(0, 2),
    ...overrides,
  }
}
