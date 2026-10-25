import { act, screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { me, problem, signedIn, signedOut } from '@/test/msw/auth'
import { server } from '@/test/msw/server'
import { renderRoute } from '@/test/renderRoute'

afterEach(() => vi.useRealTimers())

async function enterCodeAndPassword(user: ReturnType<typeof renderRoute>['user'], code = '123456') {
  await user.click(await screen.findByLabelText('Dígito 1 de 6'))
  await user.keyboard(code)
  await user.type(screen.getByLabelText('Contraseña'), 'contrasena123')
}

describe('/verificar-correo', () => {
  it('shows six code boxes and the email the code went to', async () => {
    server.use(...signedOut)
    renderRoute('/verificar-correo?email=ana%40uni.edu')

    expect(await screen.findByRole('heading', { name: 'Verifica tu correo' })).toBeInTheDocument()
    expect(screen.getByText(/ana@uni\.edu/)).toBeInTheDocument()
    for (let digit = 1; digit <= 6; digit++) {
      expect(screen.getByLabelText(`Dígito ${digit} de 6`)).toBeInTheDocument()
    }
  })

  it.each(['/verificar-correo', '/verificar-correo?email=no-es-un-correo'])(
    'sends %s to /ingresar when there is no valid email',
    async (path) => {
      server.use(...signedOut)
      const { router } = renderRoute(path)

      await waitFor(() => expect(router.state.location.pathname).toBe('/ingresar'))
    },
  )

  it('moves through the boxes while typing and accepts a pasted code', async () => {
    server.use(...signedOut)
    const { user } = renderRoute('/verificar-correo?email=ana%40uni.edu')

    await user.click(await screen.findByLabelText('Dígito 1 de 6'))
    await user.paste('987654')

    expect(screen.getByLabelText('Dígito 6 de 6')).toHaveValue('4')
    expect(screen.getByLabelText('Dígito 1 de 6')).toHaveValue('9')
  })

  it('verifies, signs in and goes to /inicio', async () => {
    let body: unknown
    server.use(
      ...signedOut,
      http.post('/api/v1/auth/verify-email', async ({ request }) => {
        body = await request.json()
        server.use(...signedIn)
        return HttpResponse.json(me)
      }),
    )
    const { user, router } = renderRoute('/verificar-correo?email=ana%40uni.edu')

    await enterCodeAndPassword(user)
    await user.click(screen.getByRole('button', { name: 'Verificar y entrar' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/inicio'))
    expect(body).toEqual({ email: 'ana@uni.edu', code: '123456', password: 'contrasena123' })
  })

  it('asks for the six digits', async () => {
    server.use(...signedOut)
    const { user } = renderRoute('/verificar-correo?email=ana%40uni.edu')

    await user.click(await screen.findByRole('button', { name: 'Verificar y entrar' }))

    expect(await screen.findByText('Ingresa los 6 dígitos del código.')).toBeInTheDocument()
  })

  it('explains an invalid code', async () => {
    server.use(
      ...signedOut,
      http.post('/api/v1/auth/verify-email', () => problem(400, 'INVALID_VERIFICATION_CODE')),
    )
    const { user } = renderRoute('/verificar-correo?email=ana%40uni.edu')

    await enterCodeAndPassword(user)
    await user.click(screen.getByRole('button', { name: 'Verificar y entrar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'El código no es válido o ya venció.',
    )
  })

  it('offers password recovery when the password does not match', async () => {
    server.use(
      ...signedOut,
      http.post('/api/v1/auth/verify-email', () => problem(400, 'VERIFICATION_PASSWORD_MISMATCH')),
    )
    const { user } = renderRoute('/verificar-correo?email=ana%40uni.edu')

    await enterCodeAndPassword(user)
    await user.click(screen.getByRole('button', { name: 'Verificar y entrar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('la contraseña no coincide')
    expect(screen.getByRole('link', { name: 'Recupera tu contraseña' })).toHaveAttribute(
      'href',
      '/recuperar-contrasena?email=ana%40uni.edu',
    )
  })

  it('lets the code be resent only after 60 seconds', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    const resend = vi.fn<() => Response>(() => new HttpResponse(null, { status: 202 }))
    server.use(...signedOut, http.post('/api/v1/auth/resend-verification', resend))
    const { user } = renderRoute('/verificar-correo?email=ana%40uni.edu')

    const button = await screen.findByRole('button', { name: /Reenviar código en \d+ s/ })
    expect(button).toBeDisabled()

    await act(() => vi.advanceTimersByTimeAsync(60_000))
    await user.click(await screen.findByRole('button', { name: 'Reenviar código' }))

    await waitFor(() => expect(resend).toHaveBeenCalledTimes(1))
    expect(await screen.findByText('Te enviamos un código nuevo.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Reenviar código en \d+ s/ })).toBeDisabled()
  })
})
