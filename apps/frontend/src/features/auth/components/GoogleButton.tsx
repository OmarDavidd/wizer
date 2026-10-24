import { useEffect, useRef, useState } from 'react'
import { loadGoogleIdentity } from '@/lib/googleIdentity'

interface GoogleButtonProps {
  clientId: string
  onCredential: (idToken: string) => void
}

/**
 * Google's own "Continuar con Google" button (GIS renders it; its look is Google's, not ours). If the
 * script cannot load (blocked, offline), the button is simply not shown.
 */
export function GoogleButton({ clientId, onCredential }: GoogleButtonProps) {
  const container = useRef<HTMLDivElement>(null)
  const callback = useRef(onCredential)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    callback.current = onCredential
  }, [onCredential])

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
        if (!cancelled) setFailed(true)
      })
    return () => {
      cancelled = true
    }
  }, [clientId])

  if (failed) return null
  return (
    <div
      ref={container}
      className="flex min-h-11 justify-center"
      data-testid="auth-google-button"
    />
  )
}
