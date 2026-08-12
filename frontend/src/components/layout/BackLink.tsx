import { Link } from 'react-router-dom'

/** The "‹ Lists" affordance at the top left of every pushed screen. */
export function BackLink({ to, label }: { to: string; label: string }) {
  return (
    <Link
      to={to}
      className="inline-flex min-h-[44px] items-center text-[13px] text-accent-700 hover:underline"
    >
      <span aria-hidden className="mr-1">
        ‹
      </span>
      {label}
    </Link>
  )
}
