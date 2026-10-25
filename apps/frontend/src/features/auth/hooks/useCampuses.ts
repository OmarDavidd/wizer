import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/api/client'

export interface CampusOption {
  id: string
  name: string
}

/**
 * Active campuses for the registration form. `GET /api/v1/campuses` belongs to the Campuses module and is
 * not in the OpenAPI document yet; when it is, this switches to the generated Orval hook.
 */
export function useCampuses() {
  return useQuery({
    queryKey: ['/api/v1/campuses'],
    queryFn: async ({ signal }) =>
      (await apiClient.get<CampusOption[]>('/api/v1/campuses', { signal })).data,
    staleTime: 60 * 60 * 1000,
  })
}
