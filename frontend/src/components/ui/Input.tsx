import { forwardRef, type InputHTMLAttributes } from 'react'
import { cx } from '@/lib/cx'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  /** The message under the field. Also flips the border to the error tone. */
  error?: string
  /** The design's inputs sit on either ground or surface, depending on the screen. */
  tone?: 'surface' | 'ground'
}

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { error, tone = 'surface', className, id, ...rest },
  ref,
) {
  const errorId = error && id ? `${id}-error` : undefined

  return (
    <div className="w-full">
      <input
        ref={ref}
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={errorId}
        className={cx(
          'w-full min-h-[50px] rounded-full px-3.5 text-sm',
          'border transition-colors placeholder:text-ink/40',
          tone === 'surface' ? 'bg-surface' : 'bg-ground',
          error ? 'border-accent-700' : 'border-divider hover:border-ink/45 focus-visible:border-accent',
          className,
        )}
        {...rest}
      />
      {error && (
        <p id={errorId} role="alert" className="mt-1.5 px-3.5 text-[11.5px] text-accent-700">
          {error}
        </p>
      )}
    </div>
  )
})
