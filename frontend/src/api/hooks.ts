import {
  useMutation,
  useQuery,
  useQueryClient,
  type UseMutationResult,
} from '@tanstack/react-query'
import { api } from './client'
import type { ListType, Member, SortMode, TodoItem, TodoListDetail, TodoListSummary } from './types'

/**
 * Query keys, in one place so an invalidation cannot miss a cache by typo.
 */
export const keys = {
  lists: ['lists'] as const,
  list: (id: string) => ['lists', id] as const,
  members: (id: string) => ['lists', id, 'members'] as const,
  types: ['list-types'] as const,
  type: (id: string) => ['list-types', id] as const,
}

// ── Queries ───────────────────────────────────────────────────────────────────

export function useLists() {
  return useQuery({
    queryKey: keys.lists,
    queryFn: ({ signal }) => api.get<TodoListSummary[]>('/api/lists', signal),
  })
}

export function useList(listId: string | undefined) {
  return useQuery({
    queryKey: keys.list(listId ?? ''),
    queryFn: ({ signal }) => api.get<TodoListDetail>(`/api/lists/${listId}`, signal),
    enabled: Boolean(listId),
  })
}

export function useListTypes() {
  return useQuery({
    queryKey: keys.types,
    queryFn: ({ signal }) => api.get<ListType[]>('/api/list-types', signal),
  })
}

export function useListType(listTypeId: string | undefined) {
  return useQuery({
    queryKey: keys.type(listTypeId ?? ''),
    queryFn: ({ signal }) => api.get<ListType>(`/api/list-types/${listTypeId}`, signal),
    enabled: Boolean(listTypeId),
  })
}

export function useMembers(listId: string | undefined) {
  return useQuery({
    queryKey: keys.members(listId ?? ''),
    queryFn: ({ signal }) => api.get<Member[]>(`/api/lists/${listId}/members`, signal),
    enabled: Boolean(listId),
  })
}

// ── Items ─────────────────────────────────────────────────────────────────────

/**
 * Ticking an item, applied to the cache immediately and rolled back if the server
 * disagrees. The design demands the row move the instant it is tapped; waiting for
 * a round trip would make the list feel dead.
 */
export function useToggleItem(listId: string) {
  const client = useQueryClient()

  return useMutation({
    mutationFn: (itemId: string) => api.post<TodoItem>(`/api/lists/${listId}/items/${itemId}/toggle`),

    onMutate: async (itemId) => {
      await client.cancelQueries({ queryKey: keys.list(listId) })
      const previous = client.getQueryData<TodoListDetail>(keys.list(listId))

      client.setQueryData<TodoListDetail>(keys.list(listId), (current) =>
        current
          ? {
              ...current,
              items: current.items.map((item) =>
                item.id === itemId ? { ...item, isCompleted: !item.isCompleted } : item,
              ),
            }
          : current,
      )

      return { previous }
    },

    onError: (_error, _itemId, context) => {
      if (context?.previous) client.setQueryData(keys.list(listId), context.previous)
    },

    // Re-read either way: the server owns the final order, and on a shared list
    // someone else may have changed something in the meantime.
    onSettled: () => {
      void client.invalidateQueries({ queryKey: keys.list(listId) })
      void client.invalidateQueries({ queryKey: keys.lists })
    },
  })
}

export function useAddItem(listId: string) {
  const client = useQueryClient()

  return useMutation({
    mutationFn: (input: { text: string; categoryId: string | null; dueOn?: string | null }) =>
      api.post<TodoItem>(`/api/lists/${listId}/items`, input),
    onSuccess: () => {
      void client.invalidateQueries({ queryKey: keys.list(listId) })
      void client.invalidateQueries({ queryKey: keys.lists })
    },
  })
}

export function useDeleteItem(listId: string) {
  const client = useQueryClient()

  return useMutation({
    mutationFn: (itemId: string) => api.delete<void>(`/api/lists/${listId}/items/${itemId}`),
    onSuccess: () => {
      void client.invalidateQueries({ queryKey: keys.list(listId) })
      void client.invalidateQueries({ queryKey: keys.lists })
    },
  })
}

// ── Lists ─────────────────────────────────────────────────────────────────────

export function useCreateList() {
  const client = useQueryClient()

  return useMutation({
    mutationFn: (input: { name: string; listTypeId: string }) =>
      api.post<TodoListDetail>('/api/lists', input),
    onSuccess: (list) => {
      client.setQueryData(keys.list(list.id), list)
      void client.invalidateQueries({ queryKey: keys.lists })
      void client.invalidateQueries({ queryKey: keys.types })
    },
  })
}

export function useSetSort(listId: string) {
  const client = useQueryClient()

  return useMutation({
    mutationFn: (sort: SortMode) => api.put<void>(`/api/lists/${listId}/sort`, { sort }),
    onSuccess: () => void client.invalidateQueries({ queryKey: keys.list(listId) }),
  })
}

export function useSetShowCompleted(listId: string) {
  const client = useQueryClient()

  return useMutation({
    mutationFn: (showCompleted: boolean) =>
      api.put<void>(`/api/lists/${listId}/show-completed`, { showCompleted }),
    // Purely a view preference: the cache is patched rather than refetched, so
    // expanding the section never costs a round trip's worth of jank.
    onMutate: (showCompleted) => {
      client.setQueryData<TodoListDetail>(keys.list(listId), (current) =>
        current ? { ...current, showCompleted } : current,
      )
    },
  })
}

// ── Types and categories ──────────────────────────────────────────────────────

export function useCreateListType() {
  const client = useQueryClient()

  return useMutation({
    mutationFn: (input: { name: string; blurb?: string | null }) =>
      api.post<ListType>('/api/list-types', input),
    onSuccess: () => void client.invalidateQueries({ queryKey: keys.types }),
  })
}

/** Category edits all return the whole type, so they share one cache update. */
function useCategoryMutation<TInput>(
  listTypeId: string,
  mutationFn: (input: TInput) => Promise<ListType>,
): UseMutationResult<ListType, Error, TInput> {
  const client = useQueryClient()

  return useMutation({
    mutationFn,
    onSuccess: (type) => {
      client.setQueryData(keys.type(listTypeId), type)
      void client.invalidateQueries({ queryKey: keys.types })
      // Category order is the custom sort, so every list of this type re-groups.
      void client.invalidateQueries({ queryKey: keys.lists })
      void client.invalidateQueries({ queryKey: ['lists'] })
    },
  })
}

export function useAddCategory(listTypeId: string) {
  return useCategoryMutation(listTypeId, (name: string) =>
    api.post<ListType>(`/api/list-types/${listTypeId}/categories`, { name }),
  )
}

export function useRenameCategory(listTypeId: string) {
  return useCategoryMutation(listTypeId, (input: { categoryId: string; name: string }) =>
    api.put<ListType>(`/api/list-types/${listTypeId}/categories/${input.categoryId}`, {
      name: input.name,
    }),
  )
}

export function useDeleteCategory(listTypeId: string) {
  return useCategoryMutation(listTypeId, (categoryId: string) =>
    api.delete<ListType>(`/api/list-types/${listTypeId}/categories/${categoryId}`),
  )
}

export function useMoveCategory(listTypeId: string) {
  return useCategoryMutation(listTypeId, (input: { categoryId: string; direction: 'up' | 'down' }) =>
    api.post<ListType>(`/api/list-types/${listTypeId}/categories/${input.categoryId}/move`, {
      direction: input.direction,
    }),
  )
}

// ── Members ───────────────────────────────────────────────────────────────────

export function useInviteMember(listId: string) {
  const client = useQueryClient()

  return useMutation({
    mutationFn: (email: string) => api.post<Member>(`/api/lists/${listId}/members`, { email }),
    onSuccess: () => {
      void client.invalidateQueries({ queryKey: keys.members(listId) })
      void client.invalidateQueries({ queryKey: keys.list(listId) })
      void client.invalidateQueries({ queryKey: keys.lists })
    },
  })
}

export function useRemoveMember(listId: string) {
  const client = useQueryClient()

  return useMutation({
    mutationFn: (memberId: string) => api.delete<void>(`/api/lists/${listId}/members/${memberId}`),
    onSuccess: () => {
      void client.invalidateQueries({ queryKey: keys.members(listId) })
      void client.invalidateQueries({ queryKey: keys.list(listId) })
      void client.invalidateQueries({ queryKey: keys.lists })
    },
  })
}
