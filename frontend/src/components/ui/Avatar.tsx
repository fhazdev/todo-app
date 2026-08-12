import type { Member } from '@/api/types'
import { cx } from '@/lib/cx'

interface AvatarStackProps {
  members: Member[]
  /** 26px on list cards, 30px in the list-detail header. */
  size?: 26 | 30
  onClick?: () => void
  className?: string
}

/**
 * The overlapping circles of initials. Each avatar carries a 2px surface-coloured
 * ring, so the overlap reads as depth rather than as a smudge.
 */
export function AvatarStack({ members, size = 26, onClick, className }: AvatarStackProps) {
  if (members.length === 0) return null

  const overlap = size === 30 ? '-9px' : '-8px'
  const fontSize = size === 30 ? 11 : 10

  const stack = (
    <div className={cx('flex items-center', className)}>
      {members.map((member, index) => (
        <span
          key={member.id}
          title={member.displayName}
          className="grid place-items-center rounded-full border-2 border-surface font-bold text-ground"
          style={{
            width: size,
            height: size,
            fontSize,
            background: member.avatarColor,
            marginLeft: index === 0 ? 0 : overlap,
          }}
        >
          {member.initials}
        </span>
      ))}
    </div>
  )

  if (!onClick) return stack

  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={`Shared with ${members.length} ${members.length === 1 ? 'person' : 'people'}`}
      className="rounded-full"
    >
      {stack}
    </button>
  )
}
