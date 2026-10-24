import { QueryClient } from '@tanstack/react-query'
import { normalizeError } from '@/api/errors'

const maxQueryRetries = 2

/** Queries retry only network errors and 5xx, twice (.ia/errors.md §TanStack Query). */
export function shouldRetryQuery(failureCount: number, error: unknown): boolean {
  if (failureCount >= maxQueryRetries) return false
  const normalized = normalizeError(error)
  return normalized.kind === 'network' || (normalized.status ?? 0) >= 500
}

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: shouldRetryQuery },
      // Mutations are never retried automatically (they may not be idempotent).
      mutations: { retry: false },
    },
  })
}
