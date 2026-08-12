import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { cx } from '@/lib/cx'

type Variant = 'primary' | 'secondary' | 'ghost'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  /** Stretches to the container, as the design's full-width buttons do. */
  block?: boolean
  children: ReactNode
}

/**
 * The system's three button styles. Hover and pressed tones come straight from the
 * handoff; every variant clears the 44px hit-target floor.
 */
const variants: Record<Variant, string> = {
  primary: 'bg-accent text-ground hover:bg-accent-600 active:bg-accent-700',
  secondary:
    'bg-ground text-ink border border-divider hover:bg-ink/7 active:bg-ink/14',
  ghost: 'text-accent hover:bg-accent/10 active:bg-accent/18',
}

export function Button({
  variant = 'primary',
  block = false,
  className,
  type = 'button',
  children,
  ...rest
}: ButtonProps) {
  return (
    <button
      type={type}
      className={cx(
        'font-heading inline-flex min-h-[44px] items-center justify-center gap-1.5',
        'rounded-full px-4 text-[15px] leading-tight transition-colors',
        'disabled:cursor-not-allowed disabled:opacity-45',
        variants[variant],
        block && 'w-full',
        className,
      )}
      {...rest}
    >
      {children}
    </button>
  )
}
