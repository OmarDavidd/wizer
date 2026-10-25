import { QueryClient } from '@tanstack/react-query'
import { describe, expect, it, vi } from 'vitest'
import { handleSessionExpired } from './sessionExpired'

function fakeRouter(pathname: string, href: string) {
  return {
    state: { location: { pathname, href } },
    navigate: vi.fn<(options: unknown) => Promise<void>>(async () => {}),
  }
}

describe('handleSessionExpired', () => {
  it('clears the cache and goes to /ingresar coming back to the current page', () => {
    const queryClient = new QueryClient()
    queryClient.setQueryData(['/api/v1/auth/me'], { id: 'u1' })
    const router = fakeRouter('/clases/42', '/clases/42?tab=chat')

    handleSessionExpired(queryClient, router)

    expect(queryClient.getQueryData(['/api/v1/auth/me'])).toBeUndefined()
    expect(router.navigate).toHaveBeenCalledWith({
      to: '/ingresar',
      search: { next: '/clases/42?tab=chat' },
    })
  })

  it('stays put on an access screen', () => {
    const router = fakeRouter('/ingresar', '/ingresar')

    handleSessionExpired(new QueryClient(), router)

    expect(router.navigate).not.toHaveBeenCalled()
  })
})
