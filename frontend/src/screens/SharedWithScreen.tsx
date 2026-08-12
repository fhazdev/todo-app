import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useInviteMember, useList, useMembers, useRemoveMember } from '@/api/hooks'
import { ApiError } from '@/api/client'
import type { Member } from '@/api/types'
import { BackLink } from '@/components/layout/BackLink'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { ErrorState, RowSkeleton } from '@/components/ui/States'

/** "Owner · you", "Can edit", "Invited". */
function role(member: Member): string {
  if (member.role === 'Owner') return member.isYou ? 'Owner · you' : 'Owner'
  return member.status === 'Invited' ? 'Invited' : 'Can edit'
}

export function SharedWithScreen() {
  const { listId = '' } = useParams()
  const { data: list } = useList(listId)
  const { data: members, isPending, error, refetch } = useMembers(listId)

  const invite = useInviteMember(listId)
  const removeMember = useRemoveMember(listId)

  const [email, setEmail] = useState('')

  function send() {
    const address = email.trim()
    if (!address) return

    invite.mutate(address, { onSuccess: () => setEmail('') })
  }

  const inviteError =
    invite.error instanceof ApiError
      ? (invite.error.fieldError('email') ?? invite.error.message)
      : undefined

  const isOwner = list?.myRole === 'Owner'

  return (
    <>
      <header className="shrink-0 bg-surface px-5 pt-3 pb-4">
        <BackLink to={`/lists/${listId}`} label={list?.name ?? 'Back'} />
        <h1 className="font-heading mt-1 text-[26px]">Shared with</h1>
      </header>

      <div className="flex-1 overflow-y-auto px-5 pt-4 pb-4 scrollbar-none">
        {isPending && <RowSkeleton rows={3} />}
        {error && <ErrorState error={error} onRetry={() => void refetch()} />}

        {members && (
          <ul className="flex flex-col gap-2.5">
            {members.map((member) => (
              <li
                key={member.id}
                className="flex min-h-[68px] items-center gap-3.5 rounded-[26px] bg-surface px-3.5 py-3"
              >
                <span
                  aria-hidden
                  className="grid h-10 w-10 shrink-0 place-items-center rounded-full text-[13px] font-bold text-ground"
                  style={{ background: member.avatarColor }}
                >
                  {member.initials}
                </span>

                <span className="min-w-0 flex-1">
                  {/* An invitation shows the address where a name would go. */}
                  <span className="block truncate text-[14.5px]">{member.displayName}</span>
                  <span className="block text-[11.5px] text-ink/55">{role(member)}</span>
                </span>

                {isOwner && member.role !== 'Owner' && (
                  <button
                    type="button"
                    aria-label={`Remove ${member.displayName}`}
                    disabled={removeMember.isPending}
                    onClick={() => removeMember.mutate(member.id)}
                    className="grid h-[38px] w-[38px] shrink-0 place-items-center rounded-full text-ink/45 transition-colors hover:bg-ink/7 disabled:opacity-30"
                  >
                    <span aria-hidden>×</span>
                  </button>
                )}
              </li>
            ))}
          </ul>
        )}

        {removeMember.error && (
          <div className="mt-3">
            <ErrorState error={removeMember.error} />
          </div>
        )}
      </div>

      {isOwner && (
        <div className="shrink-0 px-5 pb-4">
          <Input
            id="invite-email"
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') send()
            }}
            placeholder="name@email.com"
            aria-label="Invite by email"
            error={inviteError}
            className="mb-2"
          />
          <Button
            block
            className="min-h-[52px]"
            disabled={!email.trim() || invite.isPending}
            onClick={send}
          >
            Invite by email
          </Button>
        </div>
      )}
    </>
  )
}
