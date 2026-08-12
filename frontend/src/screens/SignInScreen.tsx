import { useState, type FormEvent } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { ApiError } from '@/api/client'
import { useAuth } from '@/auth/useAuth'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { GoogleSignInButton } from '@/auth/GoogleSignInButton'

/**
 * Sign in / create account. One form serves both: the primary button switches
 * between "Create account" and "Sign in", and the ghost link below swaps modes.
 */
export function SignInScreen() {
  const { user, isRestoring, signIn, register, signInWithGoogle } = useAuth()
  const location = useLocation()

  const [mode, setMode] = useState<'register' | 'signIn'>('register')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<ApiError | Error | null>(null)
  const [busy, setBusy] = useState(false)

  if (isRestoring) {
    return <div className="flex min-h-dvh items-center justify-center text-ink/45">…</div>
  }

  if (user) {
    const next = (location.state as { from?: string } | null)?.from ?? '/lists'
    return <Navigate to={next} replace />
  }

  const fieldError = (field: string) =>
    error instanceof ApiError ? error.fieldError(field) : undefined

  // Client-side checks first, so an obvious mistake never costs a round trip.
  function validate(): boolean {
    if (!/^\S+@\S+\.\S+$/.test(email)) {
      setError(new ApiError(400, 'Check your details.', { email: ['Enter a valid email address.'] }))
      return false
    }

    if (mode === 'register' && password.length < 8) {
      setError(new ApiError(400, 'Check your details.', { password: ['Use at least 8 characters.'] }))
      return false
    }

    if (!password) {
      setError(new ApiError(400, 'Check your details.', { password: ['Enter your password.'] }))
      return false
    }

    return true
  }

  async function run(action: () => Promise<void>) {
    setBusy(true)
    setError(null)

    try {
      await action()
    } catch (caught) {
      setError(caught instanceof Error ? caught : new Error('That did not work.'))
    } finally {
      setBusy(false)
    }
  }

  function onSubmit(event: FormEvent) {
    event.preventDefault()
    if (!validate()) return

    void run(() =>
      mode === 'register' ? register(email, password) : signIn(email, password),
    )
  }

  // Field errors are already shown inline; the banner is for everything else.
  const banner =
    error && !(error instanceof ApiError && Object.keys(error.fieldErrors).length > 0)
      ? error.message
      : null

  return (
    <div className="flex min-h-dvh flex-col justify-center gap-4 bg-ground px-7 pb-10">
      <div className="flex flex-col items-center gap-4">
        <div className="grid h-[72px] w-[72px] place-items-center rounded-full bg-accent">
          {/* The sprout glyph: a leaf, one corner squared off. */}
          <div className="h-7 w-7 rounded-[999px_999px_999px_4px] bg-ground" />
        </div>

        <div className="text-center">
          <h1 className="font-heading text-[38px] leading-[1.05]">Sprout</h1>
          <p className="text-[15px] text-ink/60">Lists you keep together.</p>
        </div>
      </div>

      <GoogleSignInButton
        onCredential={(idToken) => void run(() => signInWithGoogle(idToken))}
        disabled={busy}
      />

      <div className="flex items-center gap-3">
        <span className="h-px flex-1 bg-[rgb(32_30_29_/_0.14)]" />
        <span className="text-[11px] text-ink/50">or use email</span>
        <span className="h-px flex-1 bg-[rgb(32_30_29_/_0.14)]" />
      </div>

      <form onSubmit={onSubmit} className="flex flex-col gap-4" noValidate>
        <Input
          id="email"
          type="email"
          name="email"
          autoComplete="email"
          placeholder="you@email.com"
          aria-label="Email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          error={fieldError('email')}
          disabled={busy}
        />

        <Input
          id="password"
          type="password"
          name="password"
          autoComplete={mode === 'register' ? 'new-password' : 'current-password'}
          placeholder="Password"
          aria-label="Password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          error={fieldError('password')}
          disabled={busy}
        />

        {banner && (
          <p role="alert" className="text-center text-[12.5px] text-accent-700">
            {banner}
          </p>
        )}

        <Button type="submit" block disabled={busy} className="min-h-[52px]">
          {mode === 'register' ? 'Create account' : 'Sign in'}
        </Button>
      </form>

      <Button
        variant="ghost"
        className="min-h-[44px] text-[13px]"
        disabled={busy}
        onClick={() => {
          setMode(mode === 'register' ? 'signIn' : 'register')
          setError(null)
        }}
      >
        {mode === 'register' ? 'I already have an account' : 'Create an account instead'}
      </Button>
    </div>
  )
}
