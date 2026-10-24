import { createFileRoute, Outlet } from '@tanstack/react-router'
import { AuthLayout } from '@/app/layouts/AuthLayout'
import { redirectIfSignedIn } from '@/features/auth'

/** Access screens (anonymous). Someone already signed in goes on to `next` or /inicio. */
export const Route = createFileRoute('/_auth')({
  beforeLoad: ({ context, search }) =>
    redirectIfSignedIn(context.queryClient, (search as { next?: string }).next),
  component: () => (
    <AuthLayout>
      <Outlet />
    </AuthLayout>
  ),
})
