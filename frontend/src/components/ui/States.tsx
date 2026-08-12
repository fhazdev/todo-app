import type { ReactNode } from 'react'
import { ApiError } from '@/api/client'
import { Button } from './Button'

/**
 * The loading skeleton the handoff asks for: rows the shape of list cards, so the
 * screen does not jump when the real content lands.
 */
export function CardSkeleton({ rows = 3 }: { rows?: number }) {
  return (
    <div className="flex flex-col gap-3 px-5" aria-hidden>
      {Array.from({ length: rows }, (_, index) => (
        <div key={index} className="flex min-h-[82px] items-center gap-3.5 rounded-[28px] bg-surface p-[15px]">
          <div className="h-11 w-11 shrink-0 animate-pulse rounded-[999px_999px_999px_8px] bg-ink/10" />
          <div className="flex-1 space-y-2">
            <div className="h-4 w-2/5 animate-pulse rounded-full bg-ink/10" />
            <div className="h-3 w-3/5 animate-pulse rounded-full bg-ink/7" />
          </div>
        </div>
      ))}
    </div>
  )
}

export function RowSkeleton({ rows = 5 }: { rows?: number }) {
  return (
    <div className="flex flex-col" aria-hidden>
      {Array.from({ length: rows }, (_, index) => (
        <div key={index} className="flex min-h-[58px] items-center gap-3.5 border-b border-hairline px-1 py-3">
          <div className="h-[26px] w-[26px] shrink-0 animate-pulse rounded-full bg-ink/10" />
          <div className="h-4 flex-1 animate-pulse rounded-full bg-ink/10" style={{ maxWidth: `${45 + index * 8}%` }} />
        </div>
      ))}
    </div>
  )
}

/**
 * The failure state. A 401 is not shown as an error: the client is already
 * refreshing or signing out, and a red banner mid-redirect would only confuse.
 */
export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  if (error instanceof ApiError && error.isUnauthorised) return null

  const message =
    error instanceof Error ? error.message : 'Sprout could not load that. Try again.'

  return (
    <div role="alert" className="mx-5 rounded-[26px] bg-accent-200 p-4 text-center">
      <p className="text-sm text-accent-800">{message}</p>
      {onRetry && (
        <Button variant="ghost" onClick={onRetry} className="mt-1 text-[13px]">
          Try again
        </Button>
      )}
    </div>
  )
}

export function EmptyState({ title, hint, action }: { title: string; hint?: string; action?: ReactNode }) {
  return (
    <div className="flex flex-col items-center gap-2 px-8 py-14 text-center">
      <p className="font-heading text-[17px]">{title}</p>
      {hint && <p className="text-[12.5px] text-ink/55">{hint}</p>}
      {action}
    </div>
  )
}

/**
 * The offline banner. Shared lists go stale the moment the connection does, and
 * the handoff calls for saying so rather than silently serving old data.
 */
export function OfflineBanner({ online }: { online: boolean }) {
  if (online) return null

  return (
    <div role="status" className="bg-accent-700 px-5 py-1.5 text-center text-[11.5px] text-ground">
      Offline. Changes will not reach the others until you reconnect.
    </div>
  )
}
