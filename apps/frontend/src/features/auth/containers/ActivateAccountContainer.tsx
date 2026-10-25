import { useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { useState } from 'react'
import { normalizeError } from '@/api/errors'
import {
  getGetApiV1AuthMeQueryKey,
  usePostApiV1AuthSetPassword,
} from '@/api/generated/endpoints/auth/auth'
import { errorMessage } from '@/lib/errorMessages'
import { defaultAfterLogin } from '@/lib/safeNext'
import { ActivateAccountForm } from '../components/ActivateAccountForm'
import { AuthHeader } from '../components/AuthHeader'
import type { ActivateAccountValues } from '../schemas/authSchemas'
import { serverFieldErrors } from '../utils/fieldErrors'

/** A teacher or administrator invited by an administrator sets the first password (AUTH-09). */
export function ActivateAccountContainer({ token }: { token: string }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const setPassword = usePostApiV1AuthSetPassword()
  const [formError, setFormError] = useState<string | null>(null)

  async function handleSubmit(values: ActivateAccountValues) {
    setFormError(null)
    try {
      const me = await setPassword.mutateAsync({ data: { token, password: values.password } })
      queryClient.setQueryData(getGetApiV1AuthMeQueryKey(), me)
      await navigate({ href: defaultAfterLogin })
    } catch (error) {
      const apiError = normalizeError(error)
      setFormError(errorMessage(apiError))
      return serverFieldErrors(apiError, ['password'] as const)
    }
  }

  return (
    <>
      <AuthHeader title="Activa tu cuenta">Crea tu contraseña para entrar a Wizer.</AuthHeader>
      <ActivateAccountForm onSubmit={handleSubmit} formError={formError} />
    </>
  )
}
