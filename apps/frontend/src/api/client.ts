import axios, { type AxiosError, type AxiosRequestConfig } from 'axios'

/**
 * Single HTTP client. Same-origin requests carry the HttpOnly session cookies;
 * tokens are never read or stored by JavaScript (.ia/authentication.md).
 */
export const apiClient = axios.create({
  withCredentials: true,
  headers: { Accept: 'application/json' },
})

/** Orval mutator: every generated hook goes through `apiClient`. */
export const customInstance = <T>(config: AxiosRequestConfig, options?: AxiosRequestConfig) =>
  apiClient<T>({ ...config, ...options }).then(({ data }) => data)

export type ErrorType<E> = AxiosError<E>
export type BodyType<B> = B
