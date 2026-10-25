import { http, HttpResponse } from 'msw'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { server } from '@/test/msw/server'
import { apiClient, setSessionExpiredHandler } from './client'

// Also clears the "recently expired" window between tests.
afterEach(() => setSessionExpiredHandler(() => {}))

describe('apiClient 401 handling', () => {
  it('refreshes once and repeats the original request', async () => {
    let calls = 0
    const refresh = vi.fn<() => Response>(() => HttpResponse.json({}))
    server.use(
      http.get('/api/v1/things', () =>
        ++calls === 1 ? new HttpResponse(null, { status: 401 }) : HttpResponse.json({ ok: true }),
      ),
      http.post('/api/v1/auth/refresh', refresh),
    )

    const response = await apiClient.get('/api/v1/things')

    expect(response.data).toEqual({ ok: true })
    expect(refresh).toHaveBeenCalledTimes(1)
    expect(calls).toBe(2)
  })

  it('shares a single refresh between concurrent requests', async () => {
    const seen = new Map<string, number>()
    const refresh = vi.fn<() => Promise<Response>>(async () => {
      await new Promise((resolve) => setTimeout(resolve, 20))
      return HttpResponse.json({})
    })
    server.use(
      http.get('/api/v1/things/:id', ({ params }) => {
        const id = String(params.id)
        seen.set(id, (seen.get(id) ?? 0) + 1)
        return seen.get(id) === 1
          ? new HttpResponse(null, { status: 401 })
          : HttpResponse.json({ id })
      }),
      http.post('/api/v1/auth/refresh', refresh),
    )

    const results = await Promise.all(
      ['a', 'b', 'c'].map((id) => apiClient.get(`/api/v1/things/${id}`)),
    )

    expect(results.map((r) => r.data.id)).toEqual(['a', 'b', 'c'])
    expect(refresh).toHaveBeenCalledTimes(1)
  })

  it('reports the expired session once when the refresh fails', async () => {
    const onExpired = vi.fn<() => void>()
    setSessionExpiredHandler(onExpired)
    server.use(
      http.get('/api/v1/things/:id', () => new HttpResponse(null, { status: 401 })),
      http.post('/api/v1/auth/refresh', () => new HttpResponse(null, { status: 401 })),
    )

    const results = await Promise.allSettled([
      apiClient.get('/api/v1/things/a'),
      apiClient.get('/api/v1/things/b'),
    ])

    expect(results.every((r) => r.status === 'rejected')).toBe(true)
    expect(onExpired).toHaveBeenCalledTimes(1)
  })

  it.each([503, 'network'] as const)(
    'keeps the session when the refresh fails with %s (transient)',
    async (failure) => {
      const onExpired = vi.fn<() => void>()
      setSessionExpiredHandler(onExpired)
      server.use(
        http.get('/api/v1/things', () => new HttpResponse(null, { status: 401 })),
        http.post('/api/v1/auth/refresh', () =>
          failure === 'network'
            ? HttpResponse.error()
            : new HttpResponse(null, { status: failure }),
        ),
      )

      await expect(apiClient.get('/api/v1/things')).rejects.toBeDefined()
      expect(onExpired).not.toHaveBeenCalled()
    },
  )

  it('does not refresh when the repeated request is still unauthorized', async () => {
    const refresh = vi.fn<() => Response>(() => HttpResponse.json({}))
    server.use(
      http.get('/api/v1/things', () => new HttpResponse(null, { status: 401 })),
      http.post('/api/v1/auth/refresh', refresh),
    )

    await expect(apiClient.get('/api/v1/things')).rejects.toMatchObject({
      response: { status: 401 },
    })
    expect(refresh).toHaveBeenCalledTimes(1)
  })

  it.each(['login', 'register', 'verify-email', 'google', 'reset-password', 'refresh', 'logout'])(
    'never refreshes for the auth endpoint %s',
    async (endpoint) => {
      let refreshHits = 0
      server.use(
        http.post('/api/v1/auth/:endpoint', ({ params }) => {
          if (params.endpoint === 'refresh') refreshHits++
          return HttpResponse.json({ code: 'INVALID_CREDENTIALS' }, { status: 401 })
        }),
      )

      await expect(apiClient.post(`/api/v1/auth/${endpoint}`)).rejects.toMatchObject({
        response: { status: 401 },
      })
      // Only the call the test made itself: the 401 never triggers a refresh of its own.
      expect(refreshHits).toBe(endpoint === 'refresh' ? 1 : 0)
    },
  )
})
