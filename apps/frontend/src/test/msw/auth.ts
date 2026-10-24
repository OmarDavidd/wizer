import { http, HttpResponse } from 'msw'

export const me = {
  id: '01a16f9e-1b21-7e79-9bbe-9388d874dfde',
  fullName: 'Ana Pérez',
  email: 'ana@uni.edu',
  role: 'student',
  campusId: 'a983ade5-9918-40a6-b0f6-b14695d7776b',
}

/** No session: /auth/me answers 401 and the refresh fails too. */
export const signedOut = [
  http.get('/api/v1/auth/me', () => new HttpResponse(null, { status: 401 })),
  http.post('/api/v1/auth/refresh', () => new HttpResponse(null, { status: 401 })),
]

export const signedIn = [http.get('/api/v1/auth/me', () => HttpResponse.json(me))]

export function problem(status: number, code: string, extra: Record<string, unknown> = {}) {
  return HttpResponse.json(
    { status, code, ...extra },
    { status, headers: { 'Content-Type': 'application/problem+json' } },
  )
}

/** Assumed contract until the backend endpoint exists: GET /api/v1/campuses → active campuses. */
export const campusList = [
  { id: 'a983ade5-9918-40a6-b0f6-b14695d7776b', name: 'San Jacinto' },
  { id: '1262c840-712f-4cd3-a0aa-73b9214c1398', name: 'Nopala' },
  { id: '1a397b9f-331f-40bc-9bdd-8c8b2bd02131', name: 'Juxtlahuaca' },
]

export const campuses = http.get('/api/v1/campuses', () => HttpResponse.json(campusList))
