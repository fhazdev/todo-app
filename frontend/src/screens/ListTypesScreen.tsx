import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useCreateListType, useListTypes } from '@/api/hooks'
import { BackLink } from '@/components/layout/BackLink'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { CardSkeleton, ErrorState } from '@/components/ui/States'
import { ApiError } from '@/api/client'

/** "5 categories · 2 lists", correctly singular at one. */
function plural(count: number, singular: string, plural: string): string {
  return `${count} ${count === 1 ? singular : plural}`
}

export function ListTypesScreen() {
  const navigate = useNavigate()
  const { data: types, isPending, error, refetch } = useListTypes()
  const createType = useCreateListType()

  const [newTypeName, setNewTypeName] = useState('')

  async function create() {
    const name = newTypeName.trim()
    if (!name) return

    // A new type is seeded with one "Uncategorised" category, and the design opens
    // its category screen straight away so there is something to do there.
    const type = await createType.mutateAsync({ name })
    setNewTypeName('')
    void navigate(`/types/${type.id}`)
  }

  const nameError =
    createType.error instanceof ApiError
      ? (createType.error.fieldError('name') ?? createType.error.message)
      : undefined

  return (
    <>
      <header className="shrink-0 px-5 pt-3">
        <BackLink to="/lists" label="Lists" />
        <h1 className="font-heading mt-1 text-[26px]">List types</h1>
        <p className="text-[12.5px] text-ink/60">Each type carries its own categories.</p>
      </header>

      <div className="flex-1 overflow-y-auto px-5 pt-4 pb-4 scrollbar-none">
        {isPending && <CardSkeleton rows={3} />}
        {error && <ErrorState error={error} onRetry={() => void refetch()} />}

        {types && (
          <ul className="flex flex-col gap-2.5">
            {types.map((type) => (
              <li key={type.id}>
                <Link
                  to={`/types/${type.id}`}
                  className="flex min-h-[78px] items-center gap-3.5 rounded-[26px] bg-surface p-3.5 transition-colors hover:bg-neutral-300/40"
                >
                  <span
                    aria-hidden
                    className="h-10 w-10 shrink-0 rounded-[999px_999px_999px_8px]"
                    style={{ background: type.categories[0]?.color ?? '#c67139' }}
                  />

                  <span className="min-w-0 flex-1">
                    <span className="font-heading block truncate text-[16px]">{type.name}</span>
                    <span className="block text-[11.5px] text-ink/55">
                      {plural(type.categories.length, 'category', 'categories')}
                      {' · '}
                      {plural(type.listCount, 'list', 'lists')}
                    </span>
                  </span>

                  {/* Overlapping swatches, one per category. */}
                  <span aria-hidden className="flex items-center">
                    {type.categories.slice(0, 6).map((category, index) => (
                      <span
                        key={category.id}
                        className="h-4 w-4 rounded-full border-2 border-surface"
                        style={{
                          background: category.color,
                          marginLeft: index === 0 ? 0 : '-5px',
                        }}
                      />
                    ))}
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        )}

        {/* The create-a-type patch from the design. */}
        <div className="mt-4 flex flex-col gap-2.5 rounded-[26px] bg-accent-200 p-4">
          <h2 className="font-heading text-[16px] text-accent-700">Add a type of your own</h2>

          <Input
            id="new-type"
            tone="ground"
            value={newTypeName}
            onChange={(event) => setNewTypeName(event.target.value)}
            placeholder="e.g. Reading list"
            aria-label="New list type name"
            maxLength={80}
            error={nameError}
          />

          <Button
            block
            className="min-h-[48px]"
            disabled={!newTypeName.trim() || createType.isPending}
            onClick={() => void create()}
          >
            Create type
          </Button>
        </div>
      </div>
    </>
  )
}
