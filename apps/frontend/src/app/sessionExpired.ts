import type { QueryClient } from '@tanstack/react-query'
import { isAccessScreen } from '@/lib/safeNext'

interface NavigatingRouter {
  state: { location: { pathname: string; href: string } }
  navigate: (options: { to: '/ingresar'; search: { next: string } }) => Promise<void>
}

/** The refresh failed: forget the cached user data and sign in again, coming back here afterwards. */
export function handleSessionExpired(queryClient: QueryClient, router: NavigatingRouter) {
  queryClient.clear()
  const { pathname, href } = router.state.location
  if (!isAccessScreen(pathname)) {
    void router.navigate({ to: '/ingresar', search: { next: href } })
  }
}
