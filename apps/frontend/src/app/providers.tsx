import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from '@tanstack/react-router'
import { setSessionExpiredHandler } from '@/api/client'
import { createQueryClient } from '@/app/queryClient'
import { createAppRouter } from '@/app/router'
import { handleSessionExpired } from '@/app/sessionExpired'

const queryClient = createQueryClient()
const router = createAppRouter(queryClient)

setSessionExpiredHandler(() => handleSessionExpired(queryClient, router))

export function Providers() {
  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  )
}
