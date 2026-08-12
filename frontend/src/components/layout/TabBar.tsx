import { NavLink } from 'react-router-dom'
import { cx } from '@/lib/cx'

/**
 * Lucide-style icons at stroke-width 2.75, drawn inline so the app carries no icon
 * dependency for three glyphs.
 */
const icons = {
  lists: (
    <>
      <path d="M8 6h13M8 12h13M8 18h13" />
      <path d="M3 6h.01M3 12h.01M3 18h.01" />
    </>
  ),
  today: (
    <>
      <rect x="3" y="4" width="18" height="18" rx="3" />
      <path d="M16 2v4M8 2v4M3 10h18" />
    </>
  ),
  types: (
    <>
      <path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
    </>
  ),
}

const tabs = [
  { to: '/lists', label: 'Lists', icon: icons.lists },
  { to: '/today', label: 'Today', icon: icons.today },
  { to: '/types', label: 'Types', icon: icons.types },
] as const

/** The pinned bottom navigation from the Lists home screen. */
export function TabBar() {
  return (
    <nav
      aria-label="Main"
      className="shrink-0 bg-surface px-4 pt-2 pb-3"
      style={{ paddingBottom: 'max(12px, env(safe-area-inset-bottom))' }}
    >
      <ul className="flex">
        {tabs.map((tab) => (
          <li key={tab.to} className="flex-1">
            <NavLink
              to={tab.to}
              className={({ isActive }) =>
                cx(
                  'flex min-h-[56px] flex-col items-center justify-center gap-1 rounded-2xl transition-colors',
                  isActive ? 'text-accent' : 'text-ink/45',
                )
              }
            >
              {({ isActive }) => (
                <>
                  <svg
                    width="22"
                    height="22"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.75"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    aria-hidden
                  >
                    {tab.icon}
                  </svg>
                  <span className="text-[10.5px] leading-none" aria-current={isActive ? 'page' : undefined}>
                    {tab.label}
                  </span>
                </>
              )}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  )
}
