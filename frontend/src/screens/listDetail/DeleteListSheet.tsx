import { Sheet } from '@/components/ui/Sheet'
import { Button } from '@/components/ui/Button'
import { ErrorState } from '@/components/ui/States'

interface DeleteListSheetProps {
  open: boolean
  onClose: () => void
  listName: string
  itemCount: number
  /** Members other than the owner, who lose the list too. */
  sharedWithCount: number
  pending: boolean
  error: unknown
  onConfirm: () => void
}

/** "12 items", "1 item", or nothing at all when the list is empty. */
function itemPhrase(count: number): string | null {
  if (count === 0) return null
  return count === 1 ? '1 item' : `${count} items`
}

/**
 * The confirmation in front of deleting a list. Deleting cascades to every item and
 * every membership and there is no undo, so the sheet spells out what goes rather
 * than asking a bare "are you sure?".
 */
export function DeleteListSheet({
  open,
  onClose,
  listName,
  itemCount,
  sharedWithCount,
  pending,
  error,
  onConfirm,
}: DeleteListSheetProps) {
  const items = itemPhrase(itemCount)
  const shared =
    sharedWithCount > 0
      ? `${sharedWithCount} ${sharedWithCount === 1 ? 'person' : 'people'} it is shared with`
      : null

  const losing = [items, shared].filter(Boolean).join(' and ')

  return (
    <Sheet open={open} onClose={onClose} title={`Delete ${listName}?`}>
      <p className="mt-2 px-1 text-[13.5px] leading-relaxed text-ink/70">
        {losing
          ? `This removes the list, its ${losing}. It cannot be undone.`
          : 'This removes the list for good. It cannot be undone.'}
      </p>

      {error != null && (
        <div className="mt-2">
          <ErrorState error={error} />
        </div>
      )}

      <div className="mt-4 flex flex-col gap-2 px-1">
        <Button
          block
          className="min-h-[52px] bg-accent-800 hover:bg-accent-900 active:bg-accent-900"
          disabled={pending}
          onClick={onConfirm}
        >
          {pending ? 'Deleting…' : 'Delete list'}
        </Button>

        <Button variant="secondary" block className="min-h-[52px]" onClick={onClose}>
          Cancel
        </Button>
      </div>
    </Sheet>
  )
}
