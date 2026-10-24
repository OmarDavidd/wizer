import { screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { problem, signedOut } from '@/test/msw/auth'
import { server } from '@/test/msw/server'
import { renderRoute } from '@/test/renderRoute'

function recoveryApi(onReset?: (body: unknown) => Response) {
  const calls = { forgot: [] as unknown[], reset: [] as unknown[] }
  server.use(
    ...signedOut,
    http.post('/api/v1/auth/forgot-password', async ({ request }) => {
      calls.forgot.push(await request.json())
      return new HttpResponse(null, { status: 202 })
    }),
    http.post('/api/v1/auth/reset-password', async ({ request }) => {
      const body = await request.json()
      calls.reset.push(body)
      return onReset ? onReset(body) : new HttpResponse(null, { status: 204 })
    }),
  )
  return calls
}

async function requestCode(user: ReturnType<typeof renderRoute>['user']) {
  await user.type(await screen.findByLabelText('Correo'), 'ana@uni.edu')
  await user.click(screen.getByRole('button', { name: 'Enviar código' }))
}

async function resetWith(user: ReturnType<typeof renderRoute>['user'], password = 'nuevaclave456') {
  await user.click(await screen.findByLabelText('Dígito 1 de 6'))
  await user.keyboard('123456')
  await user.type(screen.getByLabelText('Nueva contraseña'), password)
  await user.click(screen.getByRole('button', { name: 'Cambiar contraseña' }))
}

describe('/recuperar-contrasena', () => {
  it('asks for the email, sends the code and moves to the reset step', async () => {
    const calls = recoveryApi()
    const { user } = renderRoute('/recuperar-contrasena')

    expect(
      await screen.findByRole('heading', { name: 'Recupera tu contraseña' }),
    ).toBeInTheDocument()
    await requestCode(user)

    expect(await screen.findByText(/Si hay una cuenta con ana@uni\.edu/)).toBeInTheDocument()
    expect(calls.forgot).toEqual([{ email: 'ana@uni.edu' }])
    expect(screen.getByLabelText('Dígito 1 de 6')).toBeInTheDocument()
  })

  it('starts with the email from the link', async () => {
    recoveryApi()
    renderRoute('/recuperar-contrasena?email=ana%40uni.edu')

    expect(await screen.findByLabelText('Correo')).toHaveValue('ana@uni.edu')
  })

  it('resets the password and invites to sign in, without signing in', async () => {
    const calls = recoveryApi()
    const { user, router } = renderRoute('/recuperar-contrasena')

    await requestCode(user)
    await resetWith(user)

    expect(
      await screen.findByRole('heading', { name: 'Listo, tu contraseña cambió' }),
    ).toBeInTheDocument()
    expect(calls.reset).toEqual([
      { email: 'ana@uni.edu', code: '123456', newPassword: 'nuevaclave456' },
    ])
    expect(screen.getByRole('link', { name: 'Iniciar sesión' })).toHaveAttribute(
      'href',
      '/ingresar',
    )
    expect(router.state.location.pathname).toBe('/recuperar-contrasena')
  })

  it('applies the password rules to the new password', async () => {
    const calls = recoveryApi()
    const { user } = renderRoute('/recuperar-contrasena')

    await requestCode(user)
    await resetWith(user, 'corta1')

    expect(await screen.findByText('Usa al menos 10 caracteres.')).toBeInTheDocument()
    expect(calls.reset).toEqual([])
  })

  it('explains an invalid code', async () => {
    recoveryApi(() => problem(400, 'INVALID_RESET_CODE'))
    const { user } = renderRoute('/recuperar-contrasena')

    await requestCode(user)
    await resetWith(user)

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'El código no es válido o ya venció.',
    )
  })

  it('validates the email before sending', async () => {
    const calls = recoveryApi()
    const { user } = renderRoute('/recuperar-contrasena')

    await user.click(await screen.findByRole('button', { name: 'Enviar código' }))

    expect(await screen.findByText('Ingresa un correo válido.')).toBeInTheDocument()
    await waitFor(() => expect(calls.forgot).toEqual([]))
  })
})
