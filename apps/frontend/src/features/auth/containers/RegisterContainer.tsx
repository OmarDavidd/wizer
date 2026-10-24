import { useNavigate } from '@tanstack/react-router'
import { useState } from 'react'
import { normalizeError } from '@/api/errors'
import { usePostApiV1AuthRegister } from '@/api/generated/endpoints/auth/auth'
import { SegmentedLinks } from '@/components/common/SegmentedLinks'
import { errorMessage } from '@/lib/errorMessages'
import { AuthHeader } from '../components/AuthHeader'
import { accessSegments } from '../constants'
import { RegisterForm } from '../components/RegisterForm'
import { useCampuses } from '../hooks/useCampuses'
import type { RegisterValues } from '../schemas/authSchemas'
import { serverFieldErrors } from '../utils/fieldErrors'
import { GoogleSignInContainer } from './GoogleSignInContainer'

export function RegisterContainer() {
  const navigate = useNavigate()
  const campuses = useCampuses()
  const registerAccount = usePostApiV1AuthRegister()
  const [formError, setFormError] = useState<string | null>(null)

  async function handleSubmit(values: RegisterValues) {
    setFormError(null)
    try {
      // Always 202, whether or not the email exists: the code (if any) arrives by email.
      await registerAccount.mutateAsync({ data: values })
      await navigate({ to: '/verificar-correo', search: { email: values.email.trim() } })
    } catch (error) {
      const apiError = normalizeError(error)
      setFormError(errorMessage(apiError))
      return serverFieldErrors(apiError, ['fullName', 'email', 'password', 'campusId'] as const)
    }
  }

  return (
    <GoogleSignInContainer>
      <AuthHeader title="Crea tu cuenta">Usa tu correo institucional.</AuthHeader>
      <SegmentedLinks label="Acceso" segments={[...accessSegments]} />
      <RegisterForm
        campuses={campuses.data}
        campusesFailed={campuses.isError}
        onSubmit={handleSubmit}
        formError={formError}
      />
    </GoogleSignInContainer>
  )
}
