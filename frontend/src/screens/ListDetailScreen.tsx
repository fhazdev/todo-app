import { useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  useAddItem,
  useDeleteList,
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
import { DeleteListSheet } from './listDetail/DeleteListSheet'
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

  const [sheet, setSheet] = useState<'sort' | 'add' | 'delete' | null>(null)

  const toggleItem = useToggleItem(listId)
  const addItem = useAddItem(listId)
  const setSort = useSetSort(listId)
  const setShowCompleted = useSetShowCompleted(listId)
  const deleteList = useDeleteList()

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

          <div className="flex items-center gap-1 pt-2">
            <AvatarStack
              members={list.members}
              size={30}
              onClick={() => void navigate(`/lists/${list.id}/members`)}
            />

            {/* Owner only, mirroring the server: an editor deleting the list out from
                under everyone else is the one destructive act sharing must not allow. */}
            {list.myRole === 'Owner' && (
              <button
                type="button"
                aria-label={`Delete ${list.name}`}
                aria-haspopup="dialog"
                onClick={() => setSheet('delete')}
                className="grid h-[38px] w-[38px] shrink-0 place-items-center rounded-full text-ink/45 transition-colors hover:bg-ink/7 hover:text-accent-800"
              >
                <svg
                  width="19"
                  height="19"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2.75"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  aria-hidden
                >
                  <path d="M3 6h18" />
                  <path d="M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2" />
                  <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                </svg>
              </button>
            )}
          </div>
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
                showChip={row.showChip}
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
              // The completed section is one flat list with no category headers, so
              // rows keep their chips here whatever the sort is.
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

      <DeleteListSheet
        open={sheet === 'delete'}
        onClose={() => {
          deleteList.reset()
          setSheet(null)
        }}
        listName={list.name}
        itemCount={list.items.length}
        sharedWithCount={list.members.filter((member) => !member.isYou).length}
        pending={deleteList.isPending}
        error={deleteList.error}
        onConfirm={() =>
          deleteList.mutate(list.id, {
            // Only on success: a failed delete keeps the sheet open with the reason,
            // rather than navigating away as though it had worked.
            onSuccess: () => void navigate('/lists', { replace: true }),
          })
        }
      />

      <AddItemSheet
        open={sheet === 'add'}
        onClose={() => setSheet(null)}
        typeName={list.type.name}
        categories={categories}
        pending={addItem.isPending}
        onAdd={(input) => {
          addItem.mutate(input)
          setSheet(null)
        }}
      />
    </>
  )
}
