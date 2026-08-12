import { useEffect, useRef, type ReactNode } from 'react'

interface SheetProps {
  open: boolean
  onClose: () => void
  title: string
  children: ReactNode
}

/**
 * The bottom sheet the sort and add-item screens use: a scrim over the list, a
 * surface panel rounded at the top only. Escape and a tap on the scrim dismiss it,
 * focus moves inside on open and returns to the trigger on close.
 */
export function Sheet({ open, onClose, title, children }: SheetProps) {
  const panelRef = useRef<HTMLDivElement>(null)
  const previouslyFocused = useRef<HTMLElement | null>(null)

  useEffect(() => {
    if (!open) return

    previouslyFocused.current = document.activeElement as HTMLElement | null

    // Focus the panel rather than the first control: the design's sheets open with
    // nothing pre-selected, and a screen reader should hear the title first.
    panelRef.current?.focus()

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault()
        onClose()
      }
    }

    document.addEventListener('keydown', onKeyDown)

    // The list behind must not scroll while a sheet is over it.
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'

    return () => {
      document.removeEventListener('keydown', onKeyDown)
      document.body.style.overflow = previousOverflow
      previouslyFocused.current?.focus()
    }
  }, [open, onClose])

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex flex-col justify-end">
      <button
        type="button"
        aria-label="Close"
        onClick={onClose}
        className="absolute inset-0 bg-[rgb(46_43_37_/_0.45)]"
      />

      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        tabIndex={-1}
        className="relative w-full rounded-t-[32px] bg-surface px-3.5 pt-[18px] pb-[26px] shadow-organic-lg outline-none"
      >
        <h2 className="font-heading px-1 text-[19px]">{title}</h2>
        {children}
      </div>
    </div>
  )
}
