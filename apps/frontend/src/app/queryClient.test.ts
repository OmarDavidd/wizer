import { AxiosError, AxiosHeaders, type AxiosResponse } from 'axios'
import { describe, expect, it } from 'vitest'
import { shouldRetryQuery } from './queryClient'

const withStatus = (status: number) =>
  new AxiosError('failed', 'ERR', undefined, undefined, {
    status,
    data: {},
    headers: {},
    config: { headers: new AxiosHeaders() },
  } as AxiosResponse)

describe('shouldRetryQuery', () => {
  it('retries network errors and 5xx up to twice', () => {
    expect(shouldRetryQuery(0, new AxiosError('Network Error', 'ERR_NETWORK'))).toBe(true)
    expect(shouldRetryQuery(1, withStatus(503))).toBe(true)
    expect(shouldRetryQuery(2, withStatus(503))).toBe(false)
  })

  it.each([400, 401, 403, 404, 409, 429])('never retries %i', (status) => {
    expect(shouldRetryQuery(0, withStatus(status))).toBe(false)
  })
})
