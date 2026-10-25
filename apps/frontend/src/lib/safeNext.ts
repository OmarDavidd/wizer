const accessScreens = [
  '/ingresar',
  '/registro',
  '/verificar-correo',
  '/recuperar-contrasena',
  '/activar',
]

export const defaultAfterLogin = '/inicio'

/** Case-insensitive and ignoring a trailing slash, like the router's own matching. */
export function isAccessScreen(pathname: string): boolean {
  const normalized = pathname.toLowerCase().replace(/\/+$/, '')
  return accessScreens.includes(normalized)
}

/**
 * The `next` destination after signing in, only if it is an internal path (a single leading `/`,
 * no `//` or `/\`) and not another access screen; otherwise `/inicio`. Prevents open redirects.
 */
export function safeNext(next: string | undefined | null): string {
  if (!next || !next.startsWith('/') || next.startsWith('//') || next.startsWith('/\\')) {
    return defaultAfterLogin
  }

  const path = next.split(/[?#]/)[0]
  return isAccessScreen(path ?? '') ? defaultAfterLogin : next
}
