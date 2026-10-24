import { describe, expect, it } from 'vitest'
import { safeNext } from './safeNext'

describe('safeNext', () => {
  it.each([
    ['/clases/123', '/clases/123'],
    ['/inicio?tab=2', '/inicio?tab=2'],
  ])('keeps an internal path %s', (next, expected) => {
    expect(safeNext(next)).toBe(expected)
  })

  it.each([
    [undefined],
    [''],
    ['https://evil.example'],
    ['//evil.example'],
    ['/\\evil.example'],
    ['clases'],
    ['/ingresar'],
    ['/registro?x=1'],
    ['/verificar-correo'],
    ['/recuperar-contrasena'],
  ])('falls back to /inicio for %s', (next) => {
    expect(safeNext(next)).toBe('/inicio')
  })
})
