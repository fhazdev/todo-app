import { useEffect, useState } from 'react'

/**
 * Tracks connectivity, so a shared list can say when it has stopped being live.
 * navigator.onLine only knows about the network interface, not whether the API is
 * reachable, which is why the banner it drives is worded as a warning rather than
 * a diagnosis.
 */
export function useOnline(): boolean {
  const [online, setOnline] = useState(() =>
    typeof navigator === 'undefined' ? true : navigator.onLine,
  )

  useEffect(() => {
    const goOnline = () => setOnline(true)
    const goOffline = () => setOnline(false)

    window.addEventListener('online', goOnline)
    window.addEventListener('offline', goOffline)

    return () => {
      window.removeEventListener('online', goOnline)
      window.removeEventListener('offline', goOffline)
    }
  }, [])

  return online
}
