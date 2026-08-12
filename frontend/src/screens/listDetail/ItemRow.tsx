import type { Category, TodoItem } from '@/api/types'
import { cx } from '@/lib/cx'
import { formatDue, PLAIN_ACCENT } from './rows'

interface ItemRowProps {
  item: TodoItem
  /** Null on a plain list: no chip, no dot, accent-coloured checkbox. */
  category: Category | null
  onToggle: () => void
}

export function ItemRow({ item, category, onToggle }: ItemRowProps) {
  const ring = category?.color ?? PLAIN_ACCENT
  const due = formatDue(item.dueOn)

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

        {(category || due) && (
          <p className="mt-[5px] flex flex-wrap items-center gap-2">
            {category && (
              <span
                className="inline-flex items-center gap-[5px] rounded-full px-[9px] py-[3px] text-[11px]"
                style={{ background: category.tint, color: category.deep }}
              >
                <span
                  aria-hidden
                  className="h-2 w-2 shrink-0 rounded-full"
                  style={{ background: category.color }}
                />
                {category.name}
              </span>
            )}
            {due && <span className="text-[11px] text-ink/50">{due}</span>}
          </p>
        )}
      </div>
    </li>
  )
}
