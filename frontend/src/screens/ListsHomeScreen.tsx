import { Link, useNavigate } from 'react-router-dom'
import { useLists } from '@/api/hooks'
import { useAuth } from '@/auth/useAuth'
import { AvatarStack } from '@/components/ui/Avatar'
import { Button } from '@/components/ui/Button'
import { CardSkeleton, EmptyState, ErrorState } from '@/components/ui/States'
import type { TodoListSummary } from '@/api/types'

/** "5 left · shared with 2", or "· just you" when nobody else is on it. */
function meta(list: TodoListSummary): string {
  const sharing =
    list.sharedWithCount > 0 ? `shared with ${list.sharedWithCount}` : 'just you'

  return `${list.openCount} left · ${sharing}`
}

export function ListsHomeScreen() {
  const { user } = useAuth()
  const { data: lists, isPending, error, refetch } = useLists()
  const navigate = useNavigate()

  return (
    <>
      <header className="px-5 pt-[22px] pb-2">
        <h1 className="font-heading text-[29px]">My lists</h1>
        <p className="text-[12.5px] text-ink/55">{user?.email}</p>
      </header>

      <div className="flex-1 overflow-y-auto pb-4 scrollbar-none">
        {isPending && <CardSkeleton />}

        {error && <ErrorState error={error} onRetry={() => void refetch()} />}

        {lists && lists.length === 0 && (
          <EmptyState
            title="No lists yet"
            hint="Make one, pick its type, and start adding."
          />
        )}

        {lists && lists.length > 0 && (
          <ul className="flex flex-col gap-3 px-5">
            {lists.map((list) => (
              <li key={list.id}>
                <Link
                  to={`/lists/${list.id}`}
                  className="flex min-h-[82px] items-center gap-3.5 rounded-[28px] bg-surface p-[15px] transition-colors hover:bg-neutral-300/40"
                >
                  {/* The type icon takes the type's first category colour. */}
                  <span
                    aria-hidden
                    className="h-11 w-11 shrink-0 rounded-[999px_999px_999px_8px]"
                    style={{ background: list.typeColor }}
                  />

                  <span className="min-w-0 flex-1">
                    <span className="font-heading block truncate text-[17px]">{list.name}</span>

                    <span className="mt-1 flex flex-wrap items-center gap-2">
                      <span
                        className="inline-flex rounded-full px-[9px] py-[2px] text-[10.5px]"
                        style={{ background: list.typeTint, color: list.typeDeep }}
                      >
                        {list.typeName}
                      </span>
                      <span className="text-xs text-ink/55">{meta(list)}</span>
                    </span>
                  </span>

                  <AvatarStack members={list.members} size={26} />
                </Link>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="px-5 pb-3">
        <Button block className="min-h-[52px]" onClick={() => void navigate('/lists/new')}>
          <span aria-hidden>＋</span> New list
        </Button>
      </div>
    </>
  )
}
