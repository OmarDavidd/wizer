import { screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { campuses, problem, signedOut } from '@/test/msw/auth'
import { server } from '@/test/msw/server'
import { renderRoute } from '@/test/renderRoute'

async function fillForm(
  user: ReturnType<typeof renderRoute>['user'],
  overrides: { password?: string; email?: string } = {},
) {
  await user.type(await screen.findByLabelText('Nombre completo'), 'Ana Pérez')
  await user.type(screen.getByLabelText('Correo'), overrides.email ?? 'ana@uni.edu')
  await user.type(screen.getByLabelText('Contraseña'), overrides.password ?? 'contrasena123')
  await user.selectOptions(await screen.findByLabelText('Campus'), 'Nopala')
}

describe('/registro', () => {
  it('shows the sign-up segment, the campuses and that it creates a student account', async () => {
    server.use(...signedOut, campuses)
    renderRoute('/registro')

    expect(await screen.findByRole('heading', { name: 'Crea tu cuenta' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Crear cuenta' })).toHaveAttribute(
      'aria-current',
      'page',
    )
    expect(await screen.findByRole('option', { name: 'San Jacinto' })).toBeInTheDocument()
    expect(screen.getByText(/El registro crea una cuenta de alumno/)).toBeInTheDocument()
    expect(screen.queryByLabelText(/rol/i)).not.toBeInTheDocument()
  })

  it.each([
    ['corta1', 'Usa al menos 10 caracteres.'],
    ['sololetrasaqui', 'Incluye al menos un número.'],
    ['1234567890', 'Incluye al menos una letra.'],
    ['ana1@uni.edu', 'La contraseña no puede ser tu correo.'],
  ])('rejects the password %s', async (password, message) => {
    server.use(...signedOut, campuses)
    const { user } = renderRoute('/registro')

    await fillForm(user, { password, email: 'ana1@uni.edu' })
    await user.click(screen.getByRole('button', { name: 'Crear cuenta' }))

    expect(await screen.findByText(message)).toBeInTheDocument()
  })

  it('asks for every field', async () => {
    server.use(...signedOut, campuses)
    const { user } = renderRoute('/registro')

    await user.click(await screen.findByRole('button', { name: 'Crear cuenta' }))

    expect(await screen.findByText('Ingresa tu nombre completo.')).toBeInTheDocument()
    expect(screen.getByText('Ingresa un correo válido.')).toBeInTheDocument()
    expect(screen.getByText('Elige tu campus.')).toBeInTheDocument()
  })

  it('registers and goes to the verification screen', async () => {
    let body: unknown
    server.use(
      ...signedOut,
      campuses,
      http.post('/api/v1/auth/register', async ({ request }) => {
        body = await request.json()
        return new HttpResponse(null, { status: 202 })
      }),
    )
    const { user, router } = renderRoute('/registro')

    await fillForm(user)
    await user.click(screen.getByRole('button', { name: 'Crear cuenta' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/verificar-correo'))
    expect(router.state.location.search).toEqual({ email: 'ana@uni.edu' })
    expect(body).toEqual({
      fullName: 'Ana Pérez',
      email: 'ana@uni.edu',
      password: 'contrasena123',
      campusId: '1262c840-712f-4cd3-a0aa-73b9214c1398',
    })
  })

  it('shows the rate limit message', async () => {
    server.use(
      ...signedOut,
      campuses,
      http.post('/api/v1/auth/register', () => problem(429, 'RATE_LIMITED')),
    )
    const { user } = renderRoute('/registro')

    await fillForm(user)
    await user.click(screen.getByRole('button', { name: 'Crear cuenta' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Hiciste demasiados pedidos seguidos.',
    )
  })

  it('says when the campuses cannot be loaded', async () => {
    server.use(
      ...signedOut,
      http.get('/api/v1/campuses', () => new HttpResponse(null, { status: 404 })),
    )
    renderRoute('/registro')

    expect(
      await screen.findByText('No pudimos cargar los campus. Recarga la página.'),
    ).toBeInTheDocument()
  })
})
