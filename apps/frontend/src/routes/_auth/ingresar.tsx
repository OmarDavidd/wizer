import { createFileRoute } from '@tanstack/react-router'
import { LoginContainer } from '@/features/auth'

export const Route = createFileRoute('/_auth/ingresar')({
  validateSearch: (search: Record<string, unknown>): { next?: string } =>
    typeof search.next === 'string' ? { next: search.next } : {},
  component: function IngresarPage() {
    const { next } = Route.useSearch()
    return <LoginContainer next={next} />
  },
})
