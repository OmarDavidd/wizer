import type { QueryClient } from '@tanstack/react-query'
import { redirect } from '@tanstack/react-router'
import { safeNext } from '@/lib/safeNext'
import { meQueryOptions } from '../hooks/useMe'

/** `beforeLoad` of protected routes: without a session, go to /ingresar and come back afterwards. */
export async function requireSession(queryClient: QueryClient, href: string) {
  const me = await queryClient.ensureQueryData(meQueryOptions)
  if (!me) {
    throw redirect({ to: '/ingresar', search: { next: href } })
  }
  return me
}

/** `beforeLoad` of the access screens: someone already signed in goes to `next` (or /inicio). */
export async function redirectIfSignedIn(queryClient: QueryClient, next: string | undefined) {
  const me = await queryClient.ensureQueryData(meQueryOptions)
  if (me) {
    throw redirect({ href: safeNext(next) })
  }
}
