import axios, {
  isAxiosError,
  type AxiosError,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios'

/**
 * Single HTTP client. Same-origin requests carry the HttpOnly session cookies;
 * tokens are never read or stored by JavaScript (.ia/authentication.md).
 * The generated paths already start with `/api/v1`, so there is no `baseURL`.
 */
export const apiClient = axios.create({
  withCredentials: true,
  headers: { Accept: 'application/json' },
})

const refreshUrl = '/api/v1/auth/refresh'

// Endpoints whose 401 means "wrong credentials", not "expired session": never refreshed.
const noRefresh =
  /^\/api\/v1\/auth\/(login|register|verify-email|resend-verification|forgot-password|reset-password|google|refresh|logout)$/

// After a failed refresh, the other 401s of the same burst are rejected without refreshing or reporting again.
const expiredGraceMs = 3000

let refreshing: Promise<void> | null = null
let expiredAt = 0
let onSessionExpired: () => void = () => {}

/** What to do when the session cannot be renewed (the app clears its cache and goes to /ingresar). */
export function setSessionExpiredHandler(handler: () => void) {
  onSessionExpired = handler
  expiredAt = 0
}

interface RetriableConfig extends InternalAxiosRequestConfig {
  _retried?: boolean
}

// One refresh shared by every request that gets a 401 at the same time; each is then repeated once.
apiClient.interceptors.response.use(undefined, async (error: AxiosError) => {
  const config = error.config as RetriableConfig | undefined
  if (
    error.response?.status !== 401 ||
    !config ||
    config._retried ||
    noRefresh.test(config.url ?? '')
  ) {
    throw error
  }

  if (Date.now() - expiredAt < expiredGraceMs) throw error

  config._retried = true
  refreshing ??= apiClient
    .post(refreshUrl)
    .then(() => {
      expiredAt = 0
    })
    .catch((refreshError: unknown) => {
      // Only a rejected refresh token ends the session; a network error or a 5xx is transient.
      if (isAxiosError(refreshError) && refreshError.response?.status === 401) {
        expiredAt = Date.now()
        onSessionExpired()
      }
      throw refreshError
    })
    .finally(() => {
      refreshing = null
    })

  try {
    await refreshing
  } catch {
    throw error
  }

  return apiClient(config)
})

/** Orval mutator: every generated hook goes through `apiClient`. */
export const customInstance = <T>(config: AxiosRequestConfig, options?: AxiosRequestConfig) =>
  apiClient<T>({ ...config, ...options }).then(({ data }) => data)

export type ErrorType<E> = AxiosError<E>
export type BodyType<B> = B
