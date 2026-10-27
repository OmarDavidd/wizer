import type { QueryClient } from '@tanstack/react-query'
import { isRedirect } from '@tanstack/react-router'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { setSessionExpiredHandler } from '@/api/client'
import { createQueryClient } from '@/app/queryClient'
import { handleSessionExpired } from '@/app/sessionExpired'
import { server } from '@/test/msw/server'
import { redirectIfSignedIn, requireSession } from './sessionGuards'

const me = { id: 'u1', fullName: 'Ana', email: 'ana@uni.edu', role: 'student', campusId: 'c1' }

function signedIn() {
  server.use(http.get('/api/v1/auth/me', () => HttpResponse.json(me)))
}

function signedOut() {
  server.use(
    http.get('/api/v1/auth/me', () => new HttpResponse(null, { status: 401 })),
    http.post('/api/v1/auth/refresh', () => new HttpResponse(null, { status: 401 })),
  )
}

async function thrown(promise: Promise<unknown>) {
  try {
    await promise
  } catch (error) {
    return error
  }
  throw new Error('Expected a redirect')
}

describe('requireSession', () => {
  it('returns the user when there is a session', async () => {
    signedIn()

    await expect(requireSession(createQueryClient(), '/inicio')).resolves.toEqual(me)
  })

  it('redirects to /ingresar with the current location as next', async () => {
    signedOut()

    const error = await thrown(requireSession(createQueryClient(), '/clases/42?tab=chat'))

    expect(isRedirect(error)).toBe(true)
    expect(error).toMatchObject({
      options: { to: '/ingresar', search: { next: '/clases/42?tab=chat' } },
    })
  })
})

describe('guards with the app session-expired handler wired', () => {
  // As in providers.tsx: a rejected refresh clears the cache, which cancels the /auth/me in flight.
  function wireSessionExpired(queryClient: QueryClient, pathname: string) {
    const router = { state: { location: { pathname, href: pathname } }, navigate: async () => {} }
    setSessionExpiredHandler(() => handleSessionExpired(queryClient, router))
  }

  it('requireSession still redirects to /ingresar on a page reload without a session', async () => {
    signedOut()
    const queryClient = createQueryClient()
    wireSessionExpired(queryClient, '/inicio')

    const error = await thrown(requireSession(queryClient, '/inicio'))

    expect(isRedirect(error)).toBe(true)
    expect(error).toMatchObject({ options: { to: '/ingresar', search: { next: '/inicio' } } })
  })

  it('redirectIfSignedIn still lets a signed-out visitor reload an access screen', async () => {
    signedOut()
    const queryClient = createQueryClient()
    wireSessionExpired(queryClient, '/ingresar')

    await expect(redirectIfSignedIn(queryClient, undefined)).resolves.toBeUndefined()
  })
})

describe('requireSession with a transient refresh failure', () => {
  it('fails with the server error instead of redirecting as if signed out', async () => {
    server.use(
      http.get('/api/v1/auth/me', () => new HttpResponse(null, { status: 401 })),
      http.post('/api/v1/auth/refresh', () => new HttpResponse(null, { status: 503 })),
    )
    const queryClient = createQueryClient()
    queryClient.setDefaultOptions({ queries: { retry: false } })

    const error = await thrown(requireSession(queryClient, '/inicio'))

    expect(isRedirect(error)).toBe(false)
    expect(error).toMatchObject({ response: { status: 503 } })
  })
})

describe('redirectIfSignedIn', () => {
  it('lets signed-out visitors stay on the access screen', async () => {
    signedOut()

    await expect(redirectIfSignedIn(createQueryClient(), undefined)).resolves.toBeUndefined()
  })

  it('sends a signed-in user to a safe next', async () => {
    signedIn()

    const error = await thrown(redirectIfSignedIn(createQueryClient(), '/clases/42'))

    expect(error).toMatchObject({ options: { href: '/clases/42' } })
  })

  it('ignores an external next', async () => {
    signedIn()

    const error = await thrown(redirectIfSignedIn(createQueryClient(), 'https://evil.example'))

    expect(error).toMatchObject({ options: { href: '/inicio' } })
  })
})
