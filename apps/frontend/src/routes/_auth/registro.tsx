import { createFileRoute } from '@tanstack/react-router'
import { RegisterContainer } from '@/features/auth'

export const Route = createFileRoute('/_auth/registro')({
  component: RegisterContainer,
})
