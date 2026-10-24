import { isAxiosError } from 'axios'

/**
 * Every API failure, normalized (.ia/errors.md §Frontend). Features never parse errors themselves:
 * they read `code` and `fieldErrors` from here and the text from `lib/errorMessages`.
 */
export interface ApiError {
  kind: 'network' | 'http'
  status?: number
  code?: string
  /** Backend validation errors by camelCase field name. */
  fieldErrors: Record<string, string[]>
  correlationId?: string
}

export function normalizeError(error: unknown): ApiError {
  if (!isAxiosError(error) || !error.response) {
    return {
      kind: 'network',
      status: undefined,
      code: undefined,
      fieldErrors: {},
      correlationId: undefined,
    }
  }

  // The generated ProblemDetails type has no `code`, `errors` or `correlationId`, so read them untyped.
  const body: Record<string, unknown> =
    typeof error.response.data === 'object' && error.response.data !== null
      ? error.response.data
      : {}

  return {
    kind: 'http',
    status: error.response.status,
    code: typeof body.code === 'string' ? body.code : undefined,
    fieldErrors: readFieldErrors(body.errors),
    correlationId: typeof body.correlationId === 'string' ? body.correlationId : undefined,
  }
}

function readFieldErrors(value: unknown): Record<string, string[]> {
  if (typeof value !== 'object' || value === null) return {}

  const fieldErrors: Record<string, string[]> = {}
  for (const [field, messages] of Object.entries(value)) {
    if (
      Array.isArray(messages) &&
      messages.length > 0 &&
      messages.every((m) => typeof m === 'string')
    ) {
      fieldErrors[field] = messages
    }
  }
  return fieldErrors
}
