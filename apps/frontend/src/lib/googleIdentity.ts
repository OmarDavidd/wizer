/** The parts of Google Identity Services (accounts.id) that the access screens use. */
export interface GoogleIdentity {
  initialize: (config: {
    client_id: string
    callback: (response: { credential: string }) => void
    ux_mode?: 'popup' | 'redirect'
  }) => void
  renderButton: (
    parent: HTMLElement,
    options: {
      type?: 'standard'
      theme?: 'outline' | 'filled_blue' | 'filled_black'
      size?: 'large' | 'medium' | 'small'
      text?: 'signin_with' | 'signup_with' | 'continue_with'
      shape?: 'rectangular' | 'pill'
      width?: number
      locale?: string
    },
  ) => void
}

declare global {
  interface Window {
    google?: { accounts: { id: GoogleIdentity } }
  }
}

const scriptUrl = 'https://accounts.google.com/gsi/client'
let loading: Promise<GoogleIdentity> | null = null

/** Loads the GIS script once, only when an access screen needs it (.ia/authentication.md §Frontend). */
export function loadGoogleIdentity(): Promise<GoogleIdentity> {
  loading ??= new Promise<GoogleIdentity>((resolve, reject) => {
    if (window.google?.accounts.id) {
      resolve(window.google.accounts.id)
      return
    }
    const script = document.createElement('script')
    script.src = scriptUrl
    script.async = true
    // A failed load is not cached: the next access screen tries again.
    const fail = (message: string) => {
      loading = null
      script.remove()
      reject(new Error(message))
    }
    script.onload = () =>
      window.google?.accounts.id ? resolve(window.google.accounts.id) : fail('GIS not available')
    script.onerror = () => fail('GIS failed to load')
    document.head.append(script)
  })
  return loading
}
