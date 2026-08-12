import { Link } from 'react-router-dom'
import type { SortMode } from '@/api/types'
import { Sheet } from '@/components/ui/Sheet'
import { cx } from '@/lib/cx'

interface SortSheetProps {
  open: boolean
  onClose: () => void
  sort: SortMode
  onChange: (sort: SortMode) => void
  typeName: string
  listTypeId: string
}

export function SortSheet({ open, onClose, sort, onChange, typeName, listTypeId }: SortSheetProps) {
  const options: Array<{ id: SortMode; label: string; note: string }> = [
    { id: 'Category', label: 'By category (custom)', note: `${typeName} categories, your order` },
    { id: 'MyOrder', label: 'My order', note: 'As you added them' },
    { id: 'DueDate', label: 'Due date', note: 'Soonest first' },
    { id: 'Alphabetical', label: 'Alphabetical', note: 'A to Z' },
  ]

  return (
    <Sheet open={open} onClose={onClose} title="Sort by">
      <ul role="radiogroup" aria-label="Sort by" className="mt-2 flex flex-col">
        {options.map((option) => {
          const selected = option.id === sort

          return (
            <li key={option.id}>
              <button
                type="button"
                role="radio"
                aria-checked={selected}
                onClick={() => {
                  onChange(option.id)
                  onClose()
                }}
                className={cx(
                  'flex min-h-[56px] w-full items-center gap-3 rounded-full px-[18px] py-3.5 text-left transition-colors',
                  selected ? 'bg-accent-200' : 'hover:bg-ink/7',
                )}
              >
                <span
                  aria-hidden
                  className="grid h-[18px] w-[18px] shrink-0 place-items-center rounded-full"
                  style={{
                    border: '2.75px solid #c67139',
                    background: selected ? '#c67139' : 'transparent',
                  }}
                />
                <span className="min-w-0">
                  <span className="block text-[14.5px]">{option.label}</span>
                  <span className="block text-[11.5px] text-ink/55">{option.note}</span>
                </span>
              </button>
            </li>
          )
        })}
      </ul>

      <Link
        to={`/types/${listTypeId}`}
        onClick={onClose}
        className="mt-2 flex min-h-[44px] items-center justify-center rounded-full text-[13px] text-accent hover:bg-accent/10"
      >
        Edit this type's categories
      </Link>
    </Sheet>
  )
}
