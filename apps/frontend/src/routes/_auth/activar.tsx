import { createFileRoute, redirect } from '@tanstack/react-router'
import { ActivateAccountContainer } from '@/features/auth'

export const Route = createFileRoute('/_auth/activar')({
  validateSearch: (search: Record<string, unknown>): { token?: string } =>
    typeof search.token === 'string' ? { token: search.token } : {},
  // Without the token from the invitation email there is nothing to activate.
  beforeLoad: ({ search }) => {
    if (!search.token) throw redirect({ to: '/ingresar' })
  },
  component: function ActivarPage() {
    const { token } = Route.useSearch()
    return <ActivateAccountContainer token={token ?? ''} />
  },
})
