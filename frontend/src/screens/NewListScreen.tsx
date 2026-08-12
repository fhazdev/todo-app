import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useCreateList, useListTypes } from '@/api/hooks'
import { BackLink } from '@/components/layout/BackLink'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { CardSkeleton, ErrorState } from '@/components/ui/States'
import { cx } from '@/lib/cx'

export function NewListScreen() {
  const navigate = useNavigate()
  const { data: types, isPending, error, refetch } = useListTypes()
  const createList = useCreateList()

  const [name, setName] = useState('')
  const [listTypeId, setListTypeId] = useState<string | null>(null)

  // Pre-select the first type, so Create always has something valid to send.
  useEffect(() => {
    if (!listTypeId && types && types.length > 0) setListTypeId(types[0].id)
  }, [types, listTypeId])

  async function create() {
    if (!listTypeId) return

    // An empty name is allowed: the server falls back to "Untitled list".
    const list = await createList.mutateAsync({ name: name.trim(), listTypeId })
    void navigate(`/lists/${list.id}`, { replace: true })
  }

  return (
    <>
      <header className="shrink-0 px-5 pt-3">
        <BackLink to="/lists" label="Lists" />
        <h1 className="font-heading mt-1 text-[26px]">New list</h1>
        <p className="text-[12.5px] text-ink/60">
          The type decides which categories items can take.
        </p>
      </header>

      <div className="flex-1 overflow-y-auto px-5 pt-4 pb-4 scrollbar-none">
        <Input
          id="list-name"
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="List name"
          aria-label="List name"
          maxLength={120}
          className="min-h-[52px]"
        />

        <p className="label-micro mt-5 mb-2 px-1">List type</p>

        {isPending && <CardSkeleton rows={3} />}
        {error && <ErrorState error={error} onRetry={() => void refetch()} />}

        {types && (
          <ul role="radiogroup" aria-label="List type" className="flex flex-col gap-2.5">
            {types.map((type) => {
              const selected = type.id === listTypeId
              const swatch = type.categories[0]

              return (
                <li key={type.id}>
                  <button
                    type="button"
                    role="radio"
                    aria-checked={selected}
                    onClick={() => setListTypeId(type.id)}
                    className={cx(
                      'flex min-h-[76px] w-full items-center gap-3.5 rounded-[26px] p-3.5 text-left transition-colors',
                    )}
                    style={{
                      background: selected ? (swatch?.tint ?? '#ffe1d0') : '#ebddc5',
                      border: `2.75px solid ${selected ? (swatch?.color ?? '#c67139') : 'transparent'}`,
                    }}
                  >
                    <span
                      aria-hidden
                      className="h-[38px] w-[38px] shrink-0 rounded-[999px_999px_999px_8px]"
                      style={{ background: swatch?.color ?? '#c67139' }}
                    />
                    <span className="min-w-0">
                      <span className="font-heading block text-[16px]">{type.name}</span>
                      <span className="block truncate text-[11.5px] text-ink/60">
                        {type.categories.map((category) => category.name).join(' · ')}
                      </span>
                    </span>
                  </button>
                </li>
              )
            })}
          </ul>
        )}

        <Button
          variant="ghost"
          className="mt-2 text-[13px]"
          onClick={() => void navigate('/types')}
        >
          <span aria-hidden>＋</span> New list type
        </Button>

        {createList.error && <ErrorState error={createList.error} />}
      </div>

      <div className="shrink-0 bg-surface px-[18px] pt-3 pb-[18px]">
        <Button
          block
          className="min-h-[54px]"
          disabled={!listTypeId || createList.isPending}
          onClick={() => void create()}
        >
          Create list
        </Button>
      </div>
    </>
  )
}
