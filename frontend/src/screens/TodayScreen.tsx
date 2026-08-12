import { Link } from 'react-router-dom'
import { useLists } from '@/api/hooks'
import { CardSkeleton, EmptyState, ErrorState } from '@/components/ui/States'

/**
 * The Today tab.
 *
 * The handoff lists Today as out of scope, so rather than ship a dead tab this
 * shows the lists that still have open items: the smallest thing that answers
 * "what needs doing" without inventing a design that has not been drawn.
 */
export function TodayScreen() {
  const { data: lists, isPending, error, refetch } = useLists()
  const active = lists?.filter((list) => list.openCount > 0) ?? []

  return (
    <>
      <header className="px-5 pt-[22px] pb-2">
        <h1 className="font-heading text-[29px]">Today</h1>
        <p className="text-[12.5px] text-ink/55">Lists with something still open.</p>
      </header>

      <div className="flex-1 overflow-y-auto pb-4 scrollbar-none">
        {isPending && <CardSkeleton rows={2} />}
        {error && <ErrorState error={error} onRetry={() => void refetch()} />}

        {lists && active.length === 0 && (
          <EmptyState title="Nothing open" hint="Everything on every list is ticked off." />
        )}

        <ul className="flex flex-col gap-3 px-5">
          {active.map((list) => (
            <li key={list.id}>
              <Link
                to={`/lists/${list.id}`}
                className="flex min-h-[68px] items-center gap-3.5 rounded-[26px] bg-surface p-3.5"
              >
                <span
                  aria-hidden
                  className="h-10 w-10 shrink-0 rounded-[999px_999px_999px_8px]"
                  style={{ background: list.typeColor }}
                />
                <span className="min-w-0 flex-1">
                  <span className="font-heading block truncate text-[16px]">{list.name}</span>
                  <span className="block text-[11.5px] text-ink/55">{list.openCount} left</span>
                </span>
              </Link>
            </li>
          ))}
        </ul>
      </div>
    </>
  )
}
