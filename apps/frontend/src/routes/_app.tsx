import { createFileRoute, Outlet } from '@tanstack/react-router'
import { requireSession } from '@/features/auth'

/** Every signed-in page. Without a session: /ingresar?next=<this page>. AppShell goes here (Luis). */
export const Route = createFileRoute('/_app')({
  beforeLoad: ({ context, location }) => requireSession(context.queryClient, location.href),
  component: Outlet,
})
