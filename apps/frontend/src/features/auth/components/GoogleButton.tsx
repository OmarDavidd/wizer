import { useEffect, useRef } from 'react'
import { loadGoogleIdentity } from '@/lib/googleIdentity'

interface GoogleButtonProps {
  clientId: string
  onCredential: (idToken: string) => void
  /** The script could not load (blocked, offline): the caller hides the whole block. */
  onUnavailable: () => void
}

/** Google's own "Continuar con Google" button: GIS renders it, so its look is Google's, not ours. */
export function GoogleButton({ clientId, onCredential, onUnavailable }: GoogleButtonProps) {
  const container = useRef<HTMLDivElement>(null)
  const callback = useRef(onCredential)
  const unavailable = useRef(onUnavailable)

  useEffect(() => {
    callback.current = onCredential
    unavailable.current = onUnavailable
  }, [onCredential, onUnavailable])

  useEffect(() => {
    let cancelled = false
    loadGoogleIdentity()
      .then((google) => {
        if (cancelled || !container.current) return
        google.initialize({
          client_id: clientId,
          callback: ({ credential }) => callback.current(credential),
          ux_mode: 'popup',
        })
        google.renderButton(container.current, {
          type: 'standard',
          theme: 'outline',
          size: 'large',
          text: 'continue_with',
          shape: 'rectangular',
          width: Math.min(container.current.clientWidth || 400, 400),
          locale: 'es',
        })
      })
      .catch(() => {
        if (!cancelled) unavailable.current()
      })
    return () => {
      cancelled = true
    }
  }, [clientId])

  return (
    <div
      ref={container}
      className="flex min-h-11 justify-center"
      data-testid="auth-google-button"
    />
  )
}
