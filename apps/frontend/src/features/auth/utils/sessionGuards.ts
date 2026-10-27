import { isCancelledError, type QueryClient } from '@tanstack/react-query'
import { redirect } from '@tanstack/react-router'
import { safeNext } from '@/lib/safeNext'
import { meQueryOptions } from '../hooks/useMe'

/**
 * The signed-in user, or `null`. A refresh rejected while this check waits clears the cache, which
 * cancels this very query; asking again is cheap, because right after an expiry the client answers
 * 401 without another refresh.
 */
async function currentSession(queryClient: QueryClient) {
  try {
    return await queryClient.ensureQueryData(meQueryOptions)
  } catch (error) {
    if (!isCancelledError(error)) throw error
    return queryClient.ensureQueryData(meQueryOptions)
  }
}

/** `beforeLoad` of protected routes: without a session, go to /ingresar and come back afterwards. */
export async function requireSession(queryClient: QueryClient, href: string) {
  const me = await currentSession(queryClient)
  if (!me) {
    throw redirect({ to: '/ingresar', search: { next: href } })
  }
  return me
}

/** `beforeLoad` of the access screens: someone already signed in goes to `next` (or /inicio). */
export async function redirectIfSignedIn(queryClient: QueryClient, next: string | undefined) {
  const me = await currentSession(queryClient)
  if (me) {
    throw redirect({ href: safeNext(next) })
  }
}
