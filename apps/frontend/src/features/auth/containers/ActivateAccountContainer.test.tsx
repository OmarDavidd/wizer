import { screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { me, problem, signedIn, signedOut } from '@/test/msw/auth'
import { server } from '@/test/msw/server'
import { renderRoute } from '@/test/renderRoute'

const teacher = { ...me, role: 'teacher' }

describe('/activar', () => {
  it('asks the invited person for a password', async () => {
    server.use(...signedOut)
    renderRoute('/activar?token=abc123')

    expect(await screen.findByRole('heading', { name: 'Activa tu cuenta' })).toBeInTheDocument()
    expect(screen.getByLabelText('Contraseña')).toBeInTheDocument()
  })

  it('sends someone without a token to /ingresar', async () => {
    server.use(...signedOut)
    const { router } = renderRoute('/activar')

    await waitFor(() => expect(router.state.location.pathname).toBe('/ingresar'))
  })

  it('sets the password with the token, signs in and goes to /inicio', async () => {
    let body: unknown
    server.use(
      ...signedOut,
      http.post('/api/v1/auth/set-password', async ({ request }) => {
        body = await request.json()
        server.use(http.get('/api/v1/auth/me', () => HttpResponse.json(teacher)))
        return HttpResponse.json(teacher)
      }),
    )
    const { user, router } = renderRoute('/activar?token=abc123')

    await user.type(await screen.findByLabelText('Contraseña'), 'nuevaclave456')
    await user.click(screen.getByRole('button', { name: 'Crear contraseña y entrar' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/inicio'))
    expect(body).toEqual({ token: 'abc123', password: 'nuevaclave456' })
  })

  it('applies the password rules', async () => {
    server.use(...signedOut)
    const { user } = renderRoute('/activar?token=abc123')

    await user.type(await screen.findByLabelText('Contraseña'), 'corta1')
    await user.click(screen.getByRole('button', { name: 'Crear contraseña y entrar' }))

    expect(await screen.findByText('Usa al menos 10 caracteres.')).toBeInTheDocument()
  })

  it('explains an invalid or expired link', async () => {
    server.use(
      ...signedOut,
      http.post('/api/v1/auth/set-password', () => problem(400, 'INVALID_OR_EXPIRED_TOKEN')),
    )
    const { user } = renderRoute('/activar?token=abc123')

    await user.type(await screen.findByLabelText('Contraseña'), 'nuevaclave456')
    await user.click(screen.getByRole('button', { name: 'Crear contraseña y entrar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'El enlace no es válido, venció o ya se usó. Pide a la administración que te reenvíe la invitación.',
    )
  })

  it('sends someone already signed in to /inicio', async () => {
    server.use(...signedIn)
    const { router } = renderRoute('/activar?token=abc123')

    await waitFor(() => expect(router.state.location.pathname).toBe('/inicio'))
  })
})
