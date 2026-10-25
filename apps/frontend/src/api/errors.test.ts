import { AxiosError, AxiosHeaders, type AxiosResponse } from 'axios'
import { describe, expect, it } from 'vitest'
import { normalizeError } from './errors'

function httpError(status: number, data: unknown): AxiosError {
  const response = {
    status,
    data,
    headers: {},
    config: { headers: new AxiosHeaders() },
  } as AxiosResponse
  return new AxiosError('Request failed', 'ERR_BAD_RESPONSE', undefined, undefined, response)
}

describe('normalizeError', () => {
  it('reads code, field errors and correlation id from a problem details body', () => {
    const error = normalizeError(
      httpError(400, {
        status: 400,
        code: 'VALIDATION_ERROR',
        errors: { password: ['Password must contain a number.'] },
        correlationId: '01JABC',
      }),
    )

    expect(error).toEqual({
      kind: 'http',
      status: 400,
      code: 'VALIDATION_ERROR',
      fieldErrors: { password: ['Password must contain a number.'] },
      correlationId: '01JABC',
    })
  })

  it('treats a request without response as a network error', () => {
    const error = normalizeError(new AxiosError('Network Error', 'ERR_NETWORK'))

    expect(error).toMatchObject({
      kind: 'network',
      status: undefined,
      code: undefined,
      fieldErrors: {},
    })
  })

  it('tolerates a body that is not problem details', () => {
    const error = normalizeError(httpError(502, '<html>Bad gateway</html>'))

    expect(error).toEqual({
      kind: 'http',
      status: 502,
      code: undefined,
      fieldErrors: {},
      correlationId: undefined,
    })
  })

  it('ignores malformed field errors', () => {
    const error = normalizeError(
      httpError(400, { code: 'VALIDATION_ERROR', errors: { email: 'not-an-array', name: [1] } }),
    )

    expect(error.fieldErrors).toEqual({})
  })

  it('wraps anything that is not an axios error as a network error', () => {
    expect(normalizeError(new Error('boom')).kind).toBe('network')
  })
})
