import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterAll, afterEach, beforeAll } from 'vitest'
import { setSessionExpiredHandler } from '@/api/client'
import { server } from './msw/server'

// Any request without a handler fails the test instead of reaching the network.
beforeAll(() => server.listen({ onUnhandledFrame: 'error' }))
afterEach(() => {
  server.resetHandlers()
  // Also clears the client's "session just expired" window, so tests never leak it to each other.
  setSessionExpiredHandler(() => {})
  cleanup()
})
afterAll(() => server.close())
