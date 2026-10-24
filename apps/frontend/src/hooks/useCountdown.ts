import { useCallback, useEffect, useState } from 'react'

/** Seconds left until `restart` can be used again; starts running with `initialSeconds`. */
export function useCountdown(initialSeconds: number) {
  const [endsAt, setEndsAt] = useState(() => Date.now() + initialSeconds * 1000)
  const [now, setNow] = useState(() => Date.now())

  useEffect(() => {
    if (now >= endsAt) return
    const timer = setInterval(() => setNow(Date.now()), 1000)
    return () => clearInterval(timer)
  }, [endsAt, now])

  const restart = useCallback((seconds: number) => {
    setEndsAt(Date.now() + seconds * 1000)
    setNow(Date.now())
  }, [])

  return { secondsLeft: Math.max(0, Math.ceil((endsAt - now) / 1000)), restart }
}
