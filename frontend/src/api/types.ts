/**
 * The wire shapes the Sprout API returns. These mirror the DTOs in
 * Sprout.Application/Common/Contracts/Dtos.cs; enums cross the wire as their
 * names, so they are string unions here rather than numbers.
 */

export type SortMode = 'Category' | 'MyOrder' | 'DueDate' | 'Alphabetical'

export type ListRole = 'Owner' | 'Editor'

export type MembershipStatus = 'Invited' | 'Active'

export interface Category {
  id: string
  name: string
  position: number
  paletteIndex: number
  /** Solid tone: category dots, checkbox rings, type icons. */
  color: string
  /** Chip background. */
  tint: string
  /** Chip and header text. */
  deep: string
}

export interface ListType {
  id: string
  name: string
  blurb: string | null
  /** Already in the type's custom order, which is what "By category" sorts on. */
  categories: Category[]
  listCount: number
  /** The account's default kind. The server returns it first; at most one is true. */
  isDefault: boolean
}

export interface Member {
  id: string
  userId: string | null
  displayName: string
  email: string | null
  initials: string
  avatarColor: string
  role: ListRole
  status: MembershipStatus
  isYou: boolean
}

export interface TodoItem {
  id: string
  text: string
  /** Null when the item is not filed under a category, which is an ordinary state. */
  categoryId: string | null
  /** How many of it. Never below 1. */
  quantity: number
  /** ISO date, or null when the item has no due date. */
  dueOn: string | null
  isCompleted: boolean
  position: number
}

export interface TodoListSummary {
  id: string
  name: string
  listTypeId: string
  typeName: string
  typeColor: string
  typeTint: string
  typeDeep: string
  openCount: number
  sharedWithCount: number
  members: Member[]
}

export interface TodoListDetail {
  id: string
  name: string
  type: ListType
  sort: SortMode
  showCompleted: boolean
  /** True when the list renders with no category chrome at all. */
  isPlain: boolean
  myRole: ListRole
  /** Already sorted: open items in the chosen order, then completed ones. */
  items: TodoItem[]
  members: Member[]
}

export interface User {
  id: string
  email: string
  displayName: string
  initials: string
  avatarColor: string
}

export interface AuthResult {
  user: User
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
}

/** RFC 9457 problem document, with the field map the API adds for validation failures. */
export interface ProblemDocument {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}
