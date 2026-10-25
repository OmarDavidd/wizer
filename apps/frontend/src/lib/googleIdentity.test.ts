import { afterEach, describe, expect, it, vi } from 'vitest'

afterEach(() => {
  vi.resetModules()
  document.head.querySelectorAll('script').forEach((script) => script.remove())
  delete window.google
})

async function freshLoader() {
  return (await import('./googleIdentity')).loadGoogleIdentity
}

describe('loadGoogleIdentity', () => {
  it('retries after a load that exposed no google object', async () => {
    const load = await freshLoader()
    const first = load()
    document.head.querySelector('script')?.dispatchEvent(new Event('load'))
    await expect(first).rejects.toThrow('GIS not available')

    const second = load()
    window.google = {
      accounts: { id: { initialize: vi.fn<() => void>(), renderButton: vi.fn<() => void>() } },
    }
    document.head.querySelector('script:last-of-type')?.dispatchEvent(new Event('load'))

    await expect(second).resolves.toBe(window.google.accounts.id)
  })

  it('loads the script only once', async () => {
    const load = await freshLoader()
    void load()
    void load()

    expect(
      document.head.querySelectorAll('script[src="https://accounts.google.com/gsi/client"]'),
    ).toHaveLength(1)
  })
})
