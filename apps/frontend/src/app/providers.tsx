import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from '@tanstack/react-router'
import { setSessionExpiredHandler } from '@/api/client'
import { createQueryClient } from '@/app/queryClient'
import { createAppRouter } from '@/app/router'
import { isAccessScreen } from '@/lib/safeNext'

const queryClient = createQueryClient()
const router = createAppRouter(queryClient)

// The refresh failed: forget the cached user data and sign in again, coming back here afterwards.
setSessionExpiredHandler(() => {
  queryClient.clear()
  const { pathname, href } = router.state.location
  if (!isAccessScreen(pathname)) {
    void router.navigate({ to: '/ingresar', search: { next: href } })
  }
})

export function Providers() {
  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  )
}
