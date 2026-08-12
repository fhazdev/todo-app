import { useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  useAddItem,
  useList,
  useSetShowCompleted,
  useSetSort,
  useToggleItem,
} from '@/api/hooks'
import type { SortMode } from '@/api/types'
import { AvatarStack } from '@/components/ui/Avatar'
import { BackLink } from '@/components/layout/BackLink'
import { EmptyState, ErrorState, RowSkeleton } from '@/components/ui/States'
import { ItemRow } from './listDetail/ItemRow'
import { AddItemSheet } from './listDetail/AddItemSheet'
import { SortSheet } from './listDetail/SortSheet'
import { buildRows } from './listDetail/rows'

/** The label on the sort pill: "Category" rather than the full option text. */
const sortLabels: Record<SortMode, string> = {
  Category: 'Category',
  MyOrder: 'My order',
  DueDate: 'Due date',
  Alphabetical: 'Alphabetical',
}

export function ListDetailScreen() {
  const { listId = '' } = useParams()
  const navigate = useNavigate()
  const { data: list, isPending, error, refetch } = useList(listId)

  const [sheet, setSheet] = useState<'sort' | 'add' | null>(null)

  const toggleItem = useToggleItem(listId)
  const addItem = useAddItem(listId)
  const setSort = useSetSort(listId)
  const setShowCompleted = useSetShowCompleted(listId)

  const open = useMemo(() => list?.items.filter((item) => !item.isCompleted) ?? [], [list])
  const completed = useMemo(() => list?.items.filter((item) => item.isCompleted) ?? [], [list])
  const rows = useMemo(() => (list ? buildRows(list, open) : []), [list, open])

  if (isPending) {
    return (
      <div className="flex-1 px-[18px] pt-4">
        <RowSkeleton />
      </div>
    )
  }

  if (error || !list) {
    return (
      <div className="flex-1 pt-6">
        <ErrorState error={error} onRetry={() => void refetch()} />
        <div className="px-5 pt-4">
          <BackLink to="/lists" label="Lists" />
        </div>
      </div>
    )
  }

  const categories = list.type.categories

  return (
    <>
      {/* ── Header ─────────────────────────────────────────────────────────── */}
      <header className="shrink-0 bg-surface px-[18px] pt-3.5 pb-3">
        <div className="flex items-start justify-between gap-3">
          <BackLink to="/lists" label="Lists" />
          <AvatarStack
            members={list.members}
            size={30}
            onClick={() => void navigate(`/lists/${list.id}/members`)}
            className="pt-2"
          />
        </div>

        <h1 className="font-heading mt-2 text-[26px]">{list.name}</h1>

        <Link to={`/types/${list.type.id}`} className="mt-1 inline-block text-xs text-accent-700">
          {list.type.name} · edit categories
        </Link>

        <div className="mt-2 flex items-center gap-3">
          <button
            type="button"
            onClick={() => setSheet('sort')}
            aria-haspopup="dialog"
            className="font-heading inline-flex min-h-[40px] items-center gap-1.5 rounded-full border border-divider bg-ground px-3.5 text-[12.5px]"
          >
            Sort: {sortLabels[list.sort]}
            <span aria-hidden>▾</span>
          </button>
          <span className="text-xs text-ink/55">{open.length} left</span>
        </div>
      </header>

      {/* ── Items ──────────────────────────────────────────────────────────── */}
      <div className="flex-1 overflow-y-auto px-[18px] pt-1.5 pb-24 scrollbar-none">
        {open.length === 0 && completed.length === 0 && (
          <EmptyState title="Nothing here yet" hint="Add your first item." />
        )}

        <ul>
          {rows.map((row) =>
            row.kind === 'header' ? (
              <li key={row.key} className="flex items-center gap-2 px-1 pt-3.5 pb-1.5">
                <span
                  aria-hidden
                  className="h-2.5 w-2.5 shrink-0 rounded-full"
                  style={{ background: row.category.color }}
                />
                <h2 className="font-heading text-sm" style={{ color: row.category.deep }}>
                  {row.category.name}
                </h2>
                <span
                  aria-hidden
                  className="h-px flex-1"
                  style={{ background: `${row.category.color}55` }}
                />
                <span className="text-[11.5px] text-ink/60">{row.count}</span>
              </li>
            ) : (
              <ItemRow
                key={row.key}
                item={row.item}
                category={row.category}
                onToggle={() => toggleItem.mutate(row.item.id)}
              />
            ),
          )}
        </ul>

        {completed.length > 0 && (
          <>
            <button
              type="button"
              onClick={() => setShowCompleted.mutate(!list.showCompleted)}
              aria-expanded={list.showCompleted}
              className="mt-5 flex min-h-[48px] items-center gap-1 text-[13px] text-neutral-700"
            >
              Completed ({completed.length}) <span aria-hidden>▾</span>
            </button>

            {list.showCompleted && (
              <ul>
                {completed.map((item) => (
                  <ItemRow
                    key={item.id}
                    item={item}
                    category={
                      list.isPlain
                        ? null
                        : (categories.find((c) => c.id === item.categoryId) ?? null)
                    }
                    onToggle={() => toggleItem.mutate(item.id)}
                  />
                ))}
              </ul>
            )}
          </>
        )}
      </div>

      {/* ── Add item ───────────────────────────────────────────────────────── */}
      <button
        type="button"
        onClick={() => setSheet('add')}
        aria-haspopup="dialog"
        className="font-heading absolute right-5 bottom-[22px] inline-flex h-[58px] items-center gap-1.5 rounded-full bg-accent px-[22px] text-[15px] text-ground shadow-organic-lg transition-colors hover:bg-accent-600 active:bg-accent-700"
      >
        <span aria-hidden>＋</span> Add item
      </button>

      <SortSheet
        open={sheet === 'sort'}
        onClose={() => setSheet(null)}
        sort={list.sort}
        onChange={(sort) => setSort.mutate(sort)}
        typeName={list.type.name}
        listTypeId={list.type.id}
      />

      <AddItemSheet
        open={sheet === 'add'}
        onClose={() => setSheet(null)}
        typeName={list.type.name}
        categories={categories}
        showCategories={!list.isPlain || categories.length > 1}
        pending={addItem.isPending}
        onAdd={(input) => {
          addItem.mutate(input)
          setSheet(null)
        }}
      />
    </>
  )
}
