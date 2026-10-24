import { screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { me, problem, signedIn, signedOut } from '@/test/msw/auth'
import { server } from '@/test/msw/server'
import { renderRoute } from '@/test/renderRoute'

async function fillAndSubmit(
  user: ReturnType<typeof renderRoute>['user'],
  email = 'ana@uni.edu',
  password = 'contrasena123',
) {
  await user.type(await screen.findByLabelText('Correo'), email)
  await user.type(screen.getByLabelText('Contraseña'), password)
  await user.click(screen.getByRole('button', { name: 'Entrar' }))
}

describe('/ingresar', () => {
  it('shows the access layout with the login segment selected', async () => {
    server.use(...signedOut)
    renderRoute('/ingresar')

    expect(await screen.findByRole('heading', { name: 'Hola de nuevo' })).toBeInTheDocument()
    expect(screen.getByText('Tu clase semipresencial, en un solo lugar.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Iniciar sesión' })).toHaveAttribute(
      'aria-current',
      'page',
    )
    expect(screen.getByRole('link', { name: 'Crear cuenta' })).toHaveAttribute('href', '/registro')
    expect(screen.getByRole('link', { name: '¿Olvidaste tu contraseña?' })).toHaveAttribute(
      'href',
      '/recuperar-contrasena',
    )
  })

  it('validates the fields before calling the API', async () => {
    server.use(...signedOut)
    const { user } = renderRoute('/ingresar')

    await user.click(await screen.findByRole('button', { name: 'Entrar' }))

    expect(await screen.findByText('Ingresa un correo válido.')).toBeInTheDocument()
    expect(screen.getByText('Ingresa tu contraseña.')).toBeInTheDocument()
    expect(screen.getByLabelText('Correo')).toHaveAttribute('aria-invalid', 'true')
  })

  it('signs in, caches the user and goes to next', async () => {
    let body: unknown
    server.use(
      ...signedOut,
      http.post('/api/v1/auth/login', async ({ request }) => {
        body = await request.json()
        server.use(...signedIn)
        return HttpResponse.json(me)
      }),
    )
    const { user, router } = renderRoute('/ingresar?next=%2Finicio%3Ftab%3D2')

    await user.click(await screen.findByLabelText('Recordarme'))
    await fillAndSubmit(user)

    await waitFor(() => expect(router.state.location.pathname).toBe('/inicio'))
    expect(router.state.location.search).toEqual({ tab: 2 })
    expect(body).toEqual({ email: 'ana@uni.edu', password: 'contrasena123', rememberMe: true })
    expect(await screen.findByRole('heading', { name: 'Hola, Ana Pérez' })).toBeInTheDocument()
  })

  it('ignores an external next', async () => {
    server.use(
      ...signedOut,
      http.post('/api/v1/auth/login', () => {
        server.use(...signedIn)
        return HttpResponse.json(me)
      }),
    )
    const { user, router } = renderRoute('/ingresar?next=https%3A%2F%2Fevil.example')

    await fillAndSubmit(user)

    await waitFor(() => expect(router.state.location.pathname).toBe('/inicio'))
  })

  it.each([
    ['INVALID_CREDENTIALS', 401, 'El correo o la contraseña no son correctos.'],
    [
      'ACCOUNT_INACTIVE',
      403,
      'Tu cuenta está desactivada. Escribe a la administración de tu institución.',
    ],
    [
      'TOO_MANY_LOGIN_ATTEMPTS',
      429,
      'Demasiados intentos fallidos. Espera unos minutos y vuelve a intentarlo.',
    ],
  ])('shows the message for %s and stays on the page', async (code, status, message) => {
    server.use(
      ...signedOut,
      http.post('/api/v1/auth/login', () => problem(status, code)),
    )
    const { user, router } = renderRoute('/ingresar')

    await fillAndSubmit(user)

    expect(await screen.findByRole('alert')).toHaveTextContent(message)
    expect(router.state.location.pathname).toBe('/ingresar')
  })

  it('sends an unverified account to the verification screen with its email', async () => {
    server.use(
      ...signedOut,
      http.post('/api/v1/auth/login', () => problem(403, 'EMAIL_NOT_VERIFIED')),
    )
    const { user, router } = renderRoute('/ingresar')

    await fillAndSubmit(user)

    await waitFor(() => expect(router.state.location.pathname).toBe('/verificar-correo'))
    expect(router.state.location.search).toEqual({ email: 'ana@uni.edu' })
  })

  it('sends someone already signed in straight to /inicio', async () => {
    server.use(...signedIn)
    const { router } = renderRoute('/ingresar')

    await waitFor(() => expect(router.state.location.pathname).toBe('/inicio'))
  })
})

describe('protected pages', () => {
  it('redirect to /ingresar with next when there is no session', async () => {
    server.use(...signedOut)
    const { router } = renderRoute('/inicio')

    await waitFor(() => expect(router.state.location.pathname).toBe('/ingresar'))
    expect(router.state.location.search).toEqual({ next: '/inicio' })
  })
})
