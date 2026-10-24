import { useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { useState } from 'react'
import { normalizeError } from '@/api/errors'
import {
  getGetApiV1AuthMeQueryKey,
  usePostApiV1AuthLogin,
} from '@/api/generated/endpoints/auth/auth'
import { errorMessage } from '@/lib/errorMessages'
import { safeNext } from '@/lib/safeNext'
import { SegmentedLinks } from '@/components/common/SegmentedLinks'
import { AuthHeader } from '../components/AuthHeader'
import { accessSegments } from '../constants'
import { LoginForm } from '../components/LoginForm'
import type { LoginValues } from '../schemas/authSchemas'
import { serverFieldErrors } from '../utils/fieldErrors'
import { GoogleSignInContainer } from './GoogleSignInContainer'

export function LoginContainer({ next }: { next?: string }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const login = usePostApiV1AuthLogin()
  const [formError, setFormError] = useState<string | null>(null)

  async function handleSubmit(values: LoginValues) {
    setFormError(null)
    try {
      const me = await login.mutateAsync({ data: values })
      queryClient.setQueryData(getGetApiV1AuthMeQueryKey(), me)
      await navigate({ href: safeNext(next) })
    } catch (error) {
      const apiError = normalizeError(error)
      if (apiError.code === 'EMAIL_NOT_VERIFIED') {
        // The API already sent a new code.
        await navigate({ to: '/verificar-correo', search: { email: values.email.trim() } })
        return
      }
      setFormError(errorMessage(apiError))
      return serverFieldErrors(apiError, ['email', 'password'] as const)
    }
  }

  return (
    <GoogleSignInContainer next={next}>
      <AuthHeader title="Hola de nuevo">Usa tu cuenta institucional.</AuthHeader>
      <SegmentedLinks label="Acceso" segments={[...accessSegments]} />
      <LoginForm onSubmit={handleSubmit} formError={formError} />
    </GoogleSignInContainer>
  )
}
