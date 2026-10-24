import { describe, expect, it } from 'vitest'
import type { ApiError } from '@/api/errors'
import { errorMessage } from './errorMessages'

const http = (status: number, code?: string, correlationId?: string): ApiError => ({
  kind: 'http',
  status,
  code,
  fieldErrors: {},
  correlationId,
})

describe('errorMessage', () => {
  it('uses the message mapped to the code', () => {
    expect(errorMessage(http(401, 'INVALID_CREDENTIALS'))).toBe(
      'El correo o la contraseña no son correctos.',
    )
  })

  it('falls back to a generic message by status for unknown codes', () => {
    expect(errorMessage(http(404, 'SOMETHING_NEW'))).toBe('No encontramos lo que buscas.')
  })

  it('adds the support code to server errors', () => {
    expect(errorMessage(http(500, 'INTERNAL_ERROR', '01JXYZ'))).toBe(
      'Algo salió mal de nuestro lado. Inténtalo de nuevo en unos minutos. Código de soporte: 01JXYZ',
    )
  })

  it('explains network errors', () => {
    expect(errorMessage({ kind: 'network', fieldErrors: {} })).toBe(
      'No pudimos conectarnos. Revisa tu conexión e inténtalo de nuevo.',
    )
  })
})
