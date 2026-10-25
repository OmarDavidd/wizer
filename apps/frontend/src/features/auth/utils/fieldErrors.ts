import type { ApiError } from '@/api/errors'

/**
 * The server's field errors for a form, in Spanish. The backend messages are technical English, and the
 * client already validates the same rules, so a server-side field error only says which field to check.
 */
export function serverFieldErrors<T extends object>(error: ApiError, fields: readonly (keyof T)[]) {
  const result: Partial<Record<keyof T, string>> = {}
  for (const field of fields) {
    if (error.fieldErrors[field as string]) result[field] = 'Revisa este campo.'
  }
  return result
}
