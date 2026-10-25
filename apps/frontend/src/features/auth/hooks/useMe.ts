import { queryOptions, useQuery } from '@tanstack/react-query'
import { normalizeError } from '@/api/errors'
import { getApiV1AuthMe, getGetApiV1AuthMeQueryKey } from '@/api/generated/endpoints/auth/auth'
import type { MeResponse } from '@/api/generated/model'

/**
 * The session (.ia/authentication.md §Frontend): the signed-in user, or `null` when there is none.
 * A 401 is not an error here: it means "signed out".
 */
export const meQueryOptions = queryOptions({
  queryKey: getGetApiV1AuthMeQueryKey(),
  queryFn: async ({ signal }): Promise<MeResponse | null> => {
    try {
      return await getApiV1AuthMe(undefined, signal)
    } catch (error) {
      if (normalizeError(error).status === 401) return null
      throw error
    }
  },
  staleTime: 5 * 60 * 1000,
})

export function useMe() {
  return useQuery(meQueryOptions)
}
