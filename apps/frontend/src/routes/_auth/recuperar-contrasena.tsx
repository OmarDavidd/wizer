import { createFileRoute } from '@tanstack/react-router'
import { RecoverPasswordContainer } from '@/features/auth'

export const Route = createFileRoute('/_auth/recuperar-contrasena')({
  validateSearch: (search: Record<string, unknown>): { email?: string } =>
    typeof search.email === 'string' ? { email: search.email } : {},
  component: function RecuperarContrasenaPage() {
    const { email } = Route.useSearch()
    return <RecoverPasswordContainer email={email} />
  },
})
