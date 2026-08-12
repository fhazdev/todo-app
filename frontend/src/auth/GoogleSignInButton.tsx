import { useEffect, useRef, useState } from 'react'
import { env } from '@/lib/env'

interface GoogleSignInButtonProps {
  onCredential: (idToken: string) => void
  disabled?: boolean
}

const CLIENT_ID = env.googleClientId
const GSI_SRC = 'https://accounts.google.com/gsi/client'

/**
 * Google Identity Services, loaded on demand and rendered as Google's own button.
 *
 * The design's "G" badge is explicitly a placeholder, and Google's branding
 * guidelines require their real asset, so the official rendered button is used
 * rather than a lookalike. Without VITE_GOOGLE_CLIENT_ID the whole control is
 * hidden, which keeps local development working with no Google project at all.
 */
export function GoogleSignInButton({ onCredential, disabled }: GoogleSignInButtonProps) {
  const container = useRef<HTMLDivElement>(null)
  const [failed, setFailed] = useState(false)
  const callback = useRef(onCredential)
  callback.current = onCredential

  useEffect(() => {
    if (!CLIENT_ID || !container.current) return

    let cancelled = false

    const render = () => {
      const google = (window as WindowWithGoogle).google
      if (cancelled || !google || !container.current) return

      google.accounts.id.initialize({
        client_id: CLIENT_ID,
        callback: (response) => callback.current(response.credential),
      })

      google.accounts.id.renderButton(container.current, {
        theme: 'outline',
        size: 'large',
        shape: 'pill',
        text: 'continue_with',
        width: container.current.clientWidth || 340,
      })
    }

    const existing = document.querySelector<HTMLScriptElement>(`script[src="${GSI_SRC}"]`)

    if (existing) {
      if ((window as WindowWithGoogle).google) render()
      else existing.addEventListener('load', render, { once: true })
      return () => {
        cancelled = true
      }
    }

    const script = document.createElement('script')
    script.src = GSI_SRC
    script.async = true
    script.defer = true
    script.onload = render
    script.onerror = () => setFailed(true)
    document.head.appendChild(script)

    return () => {
      cancelled = true
    }
  }, [])

  if (!CLIENT_ID) return null

  if (failed) {
    return (
      <p role="status" className="text-center text-[12.5px] text-ink/55">
        Sign in with Google is unavailable right now. Use your email instead.
      </p>
    )
  }

  return (
    <div
      ref={container}
      // Google's iframe cannot be disabled, so pointer events are removed instead.
      className={disabled ? 'pointer-events-none opacity-45' : undefined}
      data-testid="google-signin"
    />
  )
}

interface WindowWithGoogle extends Window {
  google?: {
    accounts: {
      id: {
        initialize: (config: {
          client_id: string
          callback: (response: { credential: string }) => void
        }) => void
        renderButton: (
          parent: HTMLElement,
          options: {
            theme: string
            size: string
            shape: string
            text: string
            width: number
          },
        ) => void
      }
    }
  }
}
