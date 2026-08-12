import { useState } from 'react'
import { useParams } from 'react-router-dom'
import {
  useAddCategory,
  useDeleteCategory,
  useListType,
  useMoveCategory,
} from '@/api/hooks'
import { ApiError } from '@/api/client'
import { BackLink } from '@/components/layout/BackLink'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { ErrorState, RowSkeleton } from '@/components/ui/States'

/**
 * The categories of one type, in the order that "By category" sorts on. Moving a
 * row here re-groups every list of this type at once.
 */
export function TypeCategoriesScreen() {
  const { listTypeId = '' } = useParams()
  const { data: type, isPending, error, refetch } = useListType(listTypeId)

  const addCategory = useAddCategory(listTypeId)
  const moveCategory = useMoveCategory(listTypeId)
  const deleteCategory = useDeleteCategory(listTypeId)

  const [newCategory, setNewCategory] = useState('')

  function add() {
    const name = newCategory.trim()
    if (!name) return

    addCategory.mutate(name, { onSuccess: () => setNewCategory('') })
  }

  const addError =
    addCategory.error instanceof ApiError
      ? (addCategory.error.fieldError('name') ?? addCategory.error.message)
      : undefined

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
          This order is what &ldquo;By category&rdquo; sorts on, for every list of this type.
        </p>
      </header>

      <div className="flex-1 overflow-y-auto px-5 pt-4 pb-4 scrollbar-none">
        <ul className="flex flex-col gap-2.5">
          {categories.map((category, index) => (
            <li
              key={category.id}
              className="flex min-h-[66px] items-center gap-3 rounded-[26px] bg-surface px-3.5 py-3"
            >
              <span className="font-heading w-3 shrink-0 text-[13px] text-ink/45">{index + 1}</span>

              <span
                aria-hidden
                className="h-8 w-8 shrink-0 rounded-full"
                style={{ background: category.color }}
              />

              <span className="font-heading min-w-0 flex-1 truncate text-[15.5px]">
                {category.name}
              </span>

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

              {/* Not in the prototype. Added because a category can otherwise never
                  be removed; items on it move to the type's catch-all server-side. */}
              <button
                type="button"
                aria-label={`Delete ${category.name}`}
                disabled={categories.length === 1 || deleteCategory.isPending}
                onClick={() => deleteCategory.mutate(category.id)}
                className="grid h-[38px] w-[38px] shrink-0 place-items-center rounded-full text-ink/45 transition-colors hover:bg-ink/7 disabled:opacity-30"
              >
                <span aria-hidden>×</span>
              </button>
            </li>
          ))}
        </ul>

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
