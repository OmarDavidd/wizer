import { screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { campuses, me, problem, signedIn, signedOut } from '@/test/msw/auth'
import { server } from '@/test/msw/server'
import { renderRoute } from '@/test/renderRoute'

// Google Identity Services, faked: the rendered button hands a credential to the registered callback.
const gis = vi.hoisted(() => ({ credential: 'google-id-token', clientId: '', fail: false }))
vi.mock('@/lib/googleIdentity', () => ({
  loadGoogleIdentity: async () => {
    if (gis.fail) throw new Error('GIS failed to load')
    return {
      initialize: (config: {
        client_id: string
        callback: (response: { credential: string }) => void
      }) => {
        gis.clientId = config.client_id
        ;(globalThis as { __gisCallback?: typeof config.callback }).__gisCallback = config.callback
      },
      renderButton: (parent: HTMLElement) => {
        const button = document.createElement('button')
        button.type = 'button'
        button.textContent = 'Continuar con Google'
        button.addEventListener('click', () =>
          (globalThis as { __gisCallback?: (r: { credential: string }) => void }).__gisCallback?.({
            credential: gis.credential,
          }),
        )
        parent.replaceChildren(button)
      },
    }
  },
}))

beforeEach(() => {
  gis.fail = false
  vi.stubEnv('VITE_GOOGLE_CLIENT_ID', 'client-123.apps.googleusercontent.com')
})
afterEach(() => vi.unstubAllEnvs())

function googleApi(respond: (body: { idToken: string; campusId?: string | null }) => Response) {
  const bodies: unknown[] = []
  server.use(
    http.post('/api/v1/auth/google', async ({ request }) => {
      const body = (await request.json()) as { idToken: string; campusId?: string | null }
      bodies.push(body)
      return respond(body)
    }),
  )
  return bodies
}

describe('Continuar con Google', () => {
  it('appears below the login form, after an "o" divider, with our client id', async () => {
    server.use(...signedOut)
    renderRoute('/ingresar')

    expect(await screen.findByRole('button', { name: 'Continuar con Google' })).toBeInTheDocument()
    expect(screen.getByText('o')).toBeInTheDocument()
    expect(gis.clientId).toBe('client-123.apps.googleusercontent.com')
  })

  it('is hidden when no client id is configured', async () => {
    vi.stubEnv('VITE_GOOGLE_CLIENT_ID', '')
    server.use(...signedOut)
    renderRoute('/ingresar')

    await screen.findByRole('heading', { name: 'Hola de nuevo' })
    expect(screen.queryByRole('button', { name: 'Continuar con Google' })).not.toBeInTheDocument()
  })

  it('hides the whole block, divider included, when the Google script cannot load', async () => {
    gis.fail = true
    server.use(...signedOut)
    renderRoute('/ingresar')

    await screen.findByRole('heading', { name: 'Hola de nuevo' })
    await waitFor(() => expect(screen.queryByText('o')).not.toBeInTheDocument())
    expect(screen.queryByRole('button', { name: 'Continuar con Google' })).not.toBeInTheDocument()
  })

  it('lets the campus step go back to the form', async () => {
    server.use(...signedOut, campuses)
    googleApi(() => problem(400, 'CAMPUS_REQUIRED'))
    const { user } = renderRoute('/registro')

    await user.click(await screen.findByRole('button', { name: 'Continuar con Google' }))
    await user.click(await screen.findByRole('button', { name: 'Volver' }))

    expect(await screen.findByRole('heading', { name: 'Crea tu cuenta' })).toBeInTheDocument()
  })

  it('signs in with an existing account and goes to next', async () => {
    server.use(...signedOut)
    const bodies = googleApi(() => {
      server.use(...signedIn)
      return HttpResponse.json(me)
    })
    const { user, router } = renderRoute('/ingresar?next=%2Fclases%2F42')

    await user.click(await screen.findByRole('button', { name: 'Continuar con Google' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/clases/42'))
    expect(bodies).toEqual([{ idToken: 'google-id-token', rememberMe: false, campusId: null }])
  })

  it('asks for the campus of a new account and retries with the same token', async () => {
    server.use(...signedOut, campuses)
    const bodies = googleApi((body) => {
      if (!body.campusId) return problem(400, 'CAMPUS_REQUIRED')
      server.use(...signedIn)
      return HttpResponse.json(me)
    })
    const { user, router } = renderRoute('/registro')

    await user.click(await screen.findByRole('button', { name: 'Continuar con Google' }))
    expect(await screen.findByRole('heading', { name: 'Un paso más' })).toBeInTheDocument()
    await user.selectOptions(await screen.findByLabelText('Campus'), 'Juxtlahuaca')
    await user.click(screen.getByRole('button', { name: 'Continuar' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/inicio'))
    expect(bodies).toEqual([
      { idToken: 'google-id-token', rememberMe: false, campusId: null },
      {
        idToken: 'google-id-token',
        rememberMe: false,
        campusId: '1a397b9f-331f-40bc-9bdd-8c8b2bd02131',
      },
    ])
  })

  it('requires choosing a campus', async () => {
    server.use(...signedOut, campuses)
    googleApi(() => problem(400, 'CAMPUS_REQUIRED'))
    const { user } = renderRoute('/registro')

    await user.click(await screen.findByRole('button', { name: 'Continuar con Google' }))
    await user.click(await screen.findByRole('button', { name: 'Continuar' }))

    expect(await screen.findByText('Elige tu campus.')).toBeInTheDocument()
  })

  it.each([
    [
      'EMAIL_ALREADY_EXISTS',
      409,
      'Ya existe una cuenta con este correo. Inicia sesión con tu contraseña.',
    ],
    ['GOOGLE_TOKEN_INVALID', 401, 'No pudimos validar tu cuenta de Google. Inténtalo de nuevo.'],
    ['ACCOUNT_INACTIVE', 403, 'Tu cuenta está desactivada.'],
  ])('explains %s', async (code, status, message) => {
    server.use(...signedOut)
    googleApi(() => problem(status, code))
    const { user } = renderRoute('/ingresar')

    await user.click(await screen.findByRole('button', { name: 'Continuar con Google' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(message)
  })
})
