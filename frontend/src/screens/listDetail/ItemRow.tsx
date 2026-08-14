import type { Category, TodoItem } from '@/api/types'
import { cx } from '@/lib/cx'
import { formatDue, PLAIN_ACCENT } from './rows'

/** Mirrors TodoItem.MinQuantity on the server, which is the real enforcement. */
const MIN_QUANTITY = 1

interface ItemRowProps {
  item: TodoItem
  /** Null on a plain list: no chip, no dot, accent-coloured checkbox. */
  category: Category | null
  /**
   * False under a category header, which already names the group. The checkbox
   * still takes the category colour, so the row keeps its tie to the group.
   */
  showChip?: boolean
  onToggle: () => void
  /** Omitted where the stepper does not belong, as in the completed section. */
  onQuantityChange?: (quantity: number) => void
}

export function ItemRow({
  item,
  category,
  showChip = true,
  onToggle,
  onQuantityChange,
}: ItemRowProps) {
  const ring = category?.color ?? PLAIN_ACCENT
  const due = formatDue(item.dueOn)
  const chip = showChip ? category : null

  return (
    <li
      className={cx(
        'flex items-start gap-3.5 px-1 py-3',
        !item.isCompleted && 'border-b border-hairline',
      )}
    >
      {/* The visible circle is 26px, per the design. The button around it is 44px,
          which is the touch target the handoff asks production to enlarge it to. */}
      <button
        type="button"
        role="checkbox"
        aria-checked={item.isCompleted}
        aria-label={item.text}
        onClick={onToggle}
        className="-m-[9px] grid h-11 w-11 shrink-0 place-items-center rounded-full"
      >
        <span
          aria-hidden
          className="grid h-[26px] w-[26px] place-items-center rounded-full transition-colors"
          style={{
            border: `2.75px solid ${ring}`,
            background: item.isCompleted ? ring : 'transparent',
          }}
        >
          {item.isCompleted && (
            <span
              className="mt-[-3px] block h-[6px] w-[11px] -rotate-45"
              style={{ borderLeft: '2.75px solid #f5ead8', borderBottom: '2.75px solid #f5ead8' }}
            />
          )}
        </span>
      </button>

      <div className="min-w-0 flex-1 pt-[3px]">
        <p
          className={cx(
            'text-[15.5px] leading-[1.3]',
            item.isCompleted && 'line-through opacity-45',
          )}
        >
          {item.text}
        </p>

        {(chip || due) && (
          <p className="mt-[5px] flex flex-wrap items-center gap-2">
            {chip && (
              <span
                className="inline-flex items-center gap-[5px] rounded-full px-[9px] py-[3px] text-[11px]"
                style={{ background: chip.tint, color: chip.deep }}
              >
                <span
                  aria-hidden
                  className="h-2 w-2 shrink-0 rounded-full"
                  style={{ background: chip.color }}
                />
                {chip.name}
              </span>
            )}
            {due && <span className="text-[11px] text-ink/50">{due}</span>}
          </p>
        )}
      </div>

      {onQuantityChange && (
        <div className="flex shrink-0 items-center gap-0.5 pt-[1px]">
          <button
            type="button"
            aria-label={`Fewer ${item.text}`}
            // The floor is 1: zero of something is a deletion, which is its own action.
            disabled={item.quantity <= MIN_QUANTITY}
            onClick={() => onQuantityChange(item.quantity - 1)}
            className="grid h-8 w-8 place-items-center rounded-full border border-divider bg-ground text-[15px] leading-none text-ink/70 transition-colors hover:bg-ink/7 disabled:opacity-30"
          >
            <span aria-hidden>−</span>
          </button>

          {/* aria-live so a screen reader hears the new count after a tap, since the
              number is the only thing that changes. */}
          <span
            aria-live="polite"
            aria-label={`Quantity ${item.quantity}`}
            className={cx(
              'min-w-[22px] text-center text-[13px] tabular-nums',
              item.isCompleted ? 'text-ink/35' : 'text-ink/70',
            )}
          >
            {item.quantity}
          </span>

          <button
            type="button"
            aria-label={`More ${item.text}`}
            onClick={() => onQuantityChange(item.quantity + 1)}
            className="grid h-8 w-8 place-items-center rounded-full border border-divider bg-ground text-[15px] leading-none text-ink/70 transition-colors hover:bg-ink/7"
          >
            <span aria-hidden>＋</span>
          </button>
        </div>
      )}
    </li>
  )
}
