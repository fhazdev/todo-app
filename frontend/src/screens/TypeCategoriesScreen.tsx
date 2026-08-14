import { useState } from 'react'
import { useParams } from 'react-router-dom'
import {
  useAddCategory,
  useDeleteCategory,
  useListType,
  useMoveCategory,
  useRenameCategory,
} from '@/api/hooks'
import { ApiError } from '@/api/client'
import { BackLink } from '@/components/layout/BackLink'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { ErrorState, RowSkeleton } from '@/components/ui/States'

/** The server's message for one field, falling back to the general one. */
function fieldMessage(error: unknown, field: string): string | undefined {
  if (!(error instanceof ApiError)) return undefined
  return error.fieldError(field) ?? error.message
}

/**
 * The categories of one type, in the order that "By category" sorts on. Moving a
 * row here re-groups every list of this type at once, and so does renaming one.
 */
export function TypeCategoriesScreen() {
  const { listTypeId = '' } = useParams()
  const { data: type, isPending, error, refetch } = useListType(listTypeId)

  const addCategory = useAddCategory(listTypeId)
  const moveCategory = useMoveCategory(listTypeId)
  const deleteCategory = useDeleteCategory(listTypeId)
  const renameCategory = useRenameCategory(listTypeId)

  const [newCategory, setNewCategory] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [draft, setDraft] = useState('')

  function add() {
    const name = newCategory.trim()
    if (!name) return

    addCategory.mutate(name, { onSuccess: () => setNewCategory('') })
  }

  function startEditing(categoryId: string, name: string) {
    renameCategory.reset()
    setEditingId(categoryId)
    setDraft(name)
  }

  function commitRename() {
    if (!editingId) return

    const name = draft.trim()
    const unchanged = type?.categories.find((c) => c.id === editingId)?.name === name

    // An empty or untouched name closes the row rather than sending a request the
    // server would only reject or no-op on.
    if (!name || unchanged) {
      setEditingId(null)
      return
    }

    renameCategory.mutate({ categoryId: editingId, name }, { onSuccess: () => setEditingId(null) })
  }

  const addError = fieldMessage(addCategory.error, 'name')
  const renameError = fieldMessage(renameCategory.error, 'name')

  if (isPending) {
    return (
      <div className="flex-1 px-5 pt-6">
        <RowSkeleton rows={4} />
      </div>
    )
  }

  if (error || !type) {
    return (
      <div className="flex-1 pt-6">
        <ErrorState error={error} onRetry={() => void refetch()} />
        <div className="px-5 pt-4">
          <BackLink to="/types" label="List types" />
        </div>
      </div>
    )
  }

  const categories = type.categories

  return (
    <>
      <header className="shrink-0 bg-surface px-5 pt-3 pb-4">
        <BackLink to="/types" label="List types" />
        <h1 className="font-heading mt-1 text-[26px]">{type.name}</h1>
        <p className="text-[12.5px] text-ink/60">
          {categories.length > 0
            ? 'This order is what “By category” sorts on, for every list of this type.'
            : 'Categories are optional. Without any, lists of this type are plain checklists.'}
        </p>
      </header>

      <div className="flex-1 overflow-y-auto px-5 pt-4 pb-4 scrollbar-none">
        <ul className="flex flex-col gap-2.5">
          {categories.map((category, index) => {
            const isEditing = editingId === category.id

            return (
              <li
                key={category.id}
                className="flex min-h-[66px] items-center gap-3 rounded-[26px] bg-surface px-3.5 py-3"
              >
                {/* The row number goes while editing, to leave the field room to
                    breathe at 412px. The colour stays, so the row is still placeable. */}
                {!isEditing && (
                  <span className="font-heading w-3 shrink-0 text-[13px] text-ink/45">
                    {index + 1}
                  </span>
                )}

                <span
                  aria-hidden
                  className="h-8 w-8 shrink-0 rounded-full"
                  style={{ background: category.color }}
                />

                {isEditing ? (
                  <>
                    <Input
                      id={`rename-${category.id}`}
                      autoFocus
                      value={draft}
                      onChange={(event) => setDraft(event.target.value)}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter') commitRename()
                        if (event.key === 'Escape') setEditingId(null)
                      }}
                      aria-label={`Rename ${category.name}`}
                      maxLength={60}
                      tone="ground"
                      error={renameError}
                      className="min-h-[42px]"
                    />

                    <Button
                      className="min-h-[42px] shrink-0 px-3.5 text-[14px]"
                      disabled={renameCategory.isPending}
                      onClick={commitRename}
                    >
                      Save
                    </Button>

                    <Button
                      variant="ghost"
                      className="min-h-[42px] shrink-0 px-2 text-[13px] text-ink/55"
                      onClick={() => setEditingId(null)}
                    >
                      Cancel
                    </Button>
                  </>
                ) : (
                  <>
                    {/* Tapping the name edits it. A pencil would be a fourth control
                        on a row that is already full at this width. */}
                    <button
                      type="button"
                      aria-label={`Rename ${category.name}`}
                      onClick={() => startEditing(category.id, category.name)}
                      className="font-heading min-w-0 flex-1 truncate rounded-lg py-1 text-left text-[15.5px] transition-colors hover:text-accent"
                    >
                      {category.name}
                    </button>

                    {/* Up on the first row and down on the last are disabled rather than
                        silent no-ops, which is what the handoff asks production to do. */}
                    <button
                      type="button"
                      aria-label={`Move ${category.name} up`}
                      disabled={index === 0 || moveCategory.isPending}
                      onClick={() => moveCategory.mutate({ categoryId: category.id, direction: 'up' })}
                      className="grid h-[38px] w-[38px] shrink-0 place-items-center rounded-full border border-divider bg-ground transition-colors hover:bg-ink/7 disabled:opacity-45"
                    >
                      <span aria-hidden>▲</span>
                    </button>

                    <button
                      type="button"
                      aria-label={`Move ${category.name} down`}
                      disabled={index === categories.length - 1 || moveCategory.isPending}
                      onClick={() => moveCategory.mutate({ categoryId: category.id, direction: 'down' })}
                      className="grid h-[38px] w-[38px] shrink-0 place-items-center rounded-full border border-divider bg-ground transition-colors hover:bg-ink/7 disabled:opacity-45"
                    >
                      <span aria-hidden>▼</span>
                    </button>

                    {/* Not in the prototype. Added because a category could otherwise
                        never be removed; items on it become uncategorised server-side.
                        The last one can go too: a type with none is a plain checklist. */}
                    <button
                      type="button"
                      aria-label={`Delete ${category.name}`}
                      disabled={deleteCategory.isPending}
                      onClick={() => deleteCategory.mutate(category.id)}
                      className="grid h-[38px] w-[38px] shrink-0 place-items-center rounded-full text-ink/45 transition-colors hover:bg-ink/7 disabled:opacity-30"
                    >
                      <span aria-hidden>×</span>
                    </button>
                  </>
                )}
              </li>
            )
          })}
        </ul>

        {categories.length === 0 && (
          <p className="rounded-[26px] bg-surface px-4 py-6 text-center text-[13px] leading-relaxed text-ink/55">
            No categories.
            <br />
            Items on these lists sit in one plain list. Add one below to start grouping.
          </p>
        )}

        {(moveCategory.error || deleteCategory.error) && (
          <div className="mt-3">
            <ErrorState error={moveCategory.error ?? deleteCategory.error} />
          </div>
        )}

        <div className="mt-4 flex items-start gap-2">
          <Input
            id="new-category"
            value={newCategory}
            onChange={(event) => setNewCategory(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') add()
            }}
            placeholder="New category"
            aria-label="New category"
            maxLength={60}
            error={addError}
          />
          <Button
            className="min-h-[50px] shrink-0"
            disabled={!newCategory.trim() || addCategory.isPending}
            onClick={add}
          >
            Add
          </Button>
        </div>
      </div>
    </>
  )
}
