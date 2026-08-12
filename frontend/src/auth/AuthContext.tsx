import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { api, session } from '@/api/client'
import type { AuthResult, User } from '@/api/types'

export interface AuthContextValue {
  user: User | null
  /** True until the stored refresh token has been tried, so routes do not flash. */
  isRestoring: boolean
  signIn: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, displayName?: string) => Promise<void>
  signInWithGoogle: (idToken: string) => Promise<void>
  signOut: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [isRestoring, setIsRestoring] = useState(true)
  const queryClient = useQueryClient()

  // On load, spend the stored refresh token for a live session. The access token
  // is only ever held in memory, so this runs on every reload.
  useEffect(() => {
    let cancelled = false

    void (async () => {
      if (await api.refreshSession()) {
        try {
          const me = await api.get<User>('/api/auth/me')
          if (!cancelled) setUser(me)
        } catch {
          session.clear()
        }
      }

      if (!cancelled) setIsRestoring(false)
    })()

    return () => {
      cancelled = true
    }
  }, [])

  const accept = useCallback((result: AuthResult) => {
    session.set(result)
    setUser(result.user)
  }, [])

  const signIn = useCallback(
    async (email: string, password: string) => {
      accept(await api.postAnonymous<AuthResult>('/api/auth/login', { email, password }))
    },
    [accept],
  )

  const register = useCallback(
    async (email: string, password: string, displayName?: string) => {
      accept(
        await api.postAnonymous<AuthResult>('/api/auth/register', {
          email,
          password,
          displayName: displayName?.trim() || null,
        }),
      )
    },
    [accept],
  )

  const signInWithGoogle = useCallback(
    async (idToken: string) => {
      accept(await api.postAnonymous<AuthResult>('/api/auth/google', { idToken }))
    },
    [accept],
  )

  const signOut = useCallback(async () => {
    const refreshToken = session.refreshToken

    if (refreshToken) {
      try {
        await api.postAnonymous<void>('/api/auth/logout', { refreshToken })
      } catch {
        // The local session is cleared either way; a failed revoke is not the
        // user's problem, and the token expires on its own.
      }
    }

    session.clear()
    setUser(null)
    queryClient.clear()
  }, [queryClient])

  const value = useMemo<AuthContextValue>(
    () => ({ user, isRestoring, signIn, register, signInWithGoogle, signOut }),
    [user, isRestoring, signIn, register, signInWithGoogle, signOut],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
