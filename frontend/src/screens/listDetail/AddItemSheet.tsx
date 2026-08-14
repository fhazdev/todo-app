import { useEffect, useState, type FormEvent } from 'react'
import type { Category } from '@/api/types'
import { Button } from '@/components/ui/Button'
import { Sheet } from '@/components/ui/Sheet'

interface AddItemSheetProps {
  open: boolean
  onClose: () => void
  typeName: string
  /** Empty when the type groups nothing, in which case no picker is drawn at all. */
  categories: Category[]
  onAdd: (input: { text: string; categoryId: string | null; dueOn: string | null }) => void
  pending?: boolean
}

export function AddItemSheet({ open, onClose, typeName, categories, onAdd, pending }: AddItemSheetProps) {
  const [text, setText] = useState('')
  const [categoryId, setCategoryId] = useState<string | null>(null)
  const [dueOn, setDueOn] = useState('')

  // Reopening starts clean and uncategorised. Nothing is pre-selected: filing an
  // item is a choice, and picking one for the user makes "no category" the harder
  // option rather than the default.
  useEffect(() => {
    if (open) {
      setText('')
      setDueOn('')
      setCategoryId(null)
    }
  }, [open])

  function onSubmit(event: FormEvent) {
    event.preventDefault()

    // An empty input closes without adding, rather than complaining.
    if (!text.trim()) {
      onClose()
      return
    }

    onAdd({ text: text.trim(), categoryId, dueOn: dueOn || null })
  }

  return (
    <Sheet open={open} onClose={onClose} title="New item">
      <form onSubmit={onSubmit} className="mt-3 flex flex-col gap-3 px-1">
        <input
          autoFocus
          value={text}
          onChange={(event) => setText(event.target.value)}
          placeholder="What needs doing?"
          aria-label="What needs doing?"
          maxLength={500}
          className="min-h-[50px] w-full rounded-full border border-divider bg-ground px-3.5 text-sm placeholder:text-ink/40 focus-visible:border-accent"
        />

        {categories.length > 0 && (
          <>
            <p className="label-micro px-1">{typeName} categories</p>

            <div className="flex flex-wrap gap-2">
              {/* Uncategorised is a real choice, so it gets a chip like the rest
                  rather than being the state you land in by deselecting. */}
              <button
                type="button"
                aria-pressed={categoryId === null}
                onClick={() => setCategoryId(null)}
                className="inline-flex min-h-[40px] items-center rounded-full border border-divider px-3.5 py-2 text-[13px] transition-colors"
                style={
                  categoryId === null
                    ? { background: '#645c50', color: '#f5ead8', borderColor: '#645c50' }
                    : { background: 'transparent', color: '#645c50' }
                }
              >
                No category
              </button>

              {categories.map((category) => {
                const selected = category.id === categoryId

                return (
                  <button
                    key={category.id}
                    type="button"
                    aria-pressed={selected}
                    onClick={() => setCategoryId(category.id)}
                    className="inline-flex min-h-[40px] items-center rounded-full px-3.5 py-2 text-[13px] transition-colors"
                    style={
                      selected
                        ? { background: category.color, color: '#f5ead8' }
                        : { background: category.tint, color: category.deep }
                    }
                  >
                    {category.name}
                  </button>
                )
              })}
            </div>
          </>
        )}

        {/* Due dates are shown on rows in the prototype but could not be set there.
            The field is offered here so the display is not decorative. */}
        <label className="flex items-center justify-between gap-3 px-1 text-[12.5px] text-ink/55">
          Due date
          <input
            type="date"
            value={dueOn}
            onChange={(event) => setDueOn(event.target.value)}
            className="min-h-[40px] rounded-full border border-divider bg-ground px-3 text-[13px] text-ink"
          />
        </label>

        <div className="mt-1 flex gap-2">
          <Button variant="secondary" block className="min-h-[50px]" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" block className="min-h-[50px]" disabled={pending}>
            Add
          </Button>
        </div>
      </form>
    </Sheet>
  )
}
