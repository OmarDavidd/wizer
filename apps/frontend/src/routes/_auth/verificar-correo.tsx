import { createFileRoute, redirect } from '@tanstack/react-router'
import { VerifyEmailContainer } from '@/features/auth'

export const Route = createFileRoute('/_auth/verificar-correo')({
  validateSearch: (search: Record<string, unknown>): { email?: string } =>
    typeof search.email === 'string' ? { email: search.email } : {},
  // Without the email there is no code to check: start from the sign-in screen.
  beforeLoad: ({ search }) => {
    if (!search.email) throw redirect({ to: '/ingresar' })
  },
  component: function VerificarCorreoPage() {
    const { email } = Route.useSearch()
    return <VerifyEmailContainer email={email ?? ''} />
  },
})
