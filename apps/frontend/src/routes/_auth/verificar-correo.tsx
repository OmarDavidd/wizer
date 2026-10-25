import { createFileRoute, redirect } from '@tanstack/react-router'
import { isValidEmail, VerifyEmailContainer } from '@/features/auth'

export const Route = createFileRoute('/_auth/verificar-correo')({
  validateSearch: (search: Record<string, unknown>): { email?: string } =>
    typeof search.email === 'string' ? { email: search.email } : {},
  // Without a valid email there is no code to check (the form does not show that field): start over.
  beforeLoad: ({ search }) => {
    if (!isValidEmail(search.email)) throw redirect({ to: '/ingresar' })
  },
  component: function VerificarCorreoPage() {
    const { email } = Route.useSearch()
    return <VerifyEmailContainer email={email ?? ''} />
  },
})
