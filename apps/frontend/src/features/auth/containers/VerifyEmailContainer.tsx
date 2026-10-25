import { useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate } from '@tanstack/react-router'
import { useState } from 'react'
import { type ApiError, normalizeError } from '@/api/errors'
import {
  getGetApiV1AuthMeQueryKey,
  usePostApiV1AuthResendVerification,
  usePostApiV1AuthVerifyEmail,
} from '@/api/generated/endpoints/auth/auth'
import { useCountdown } from '@/hooks/useCountdown'
import { errorMessage } from '@/lib/errorMessages'
import { defaultAfterLogin } from '@/lib/safeNext'
import { AuthHeader } from '../components/AuthHeader'
import { FormAlert } from '../components/FormAlert'
import { ResendCodeButton } from '../components/ResendCodeButton'
import { VerifyEmailForm } from '../components/VerifyEmailForm'
import type { VerifyEmailValues } from '../schemas/authSchemas'
import { serverFieldErrors } from '../utils/fieldErrors'

// The API accepts a new code only 60 s after the previous one (.ia/authentication.md).
const resendCooldownSeconds = 60

export function VerifyEmailContainer({ email }: { email: string }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const verify = usePostApiV1AuthVerifyEmail()
  const resend = usePostApiV1AuthResendVerification()
  const countdown = useCountdown(resendCooldownSeconds)
  const [error, setError] = useState<ApiError | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  async function handleSubmit(values: VerifyEmailValues) {
    setError(null)
    try {
      const me = await verify.mutateAsync({ data: values })
      queryClient.setQueryData(getGetApiV1AuthMeQueryKey(), me)
      await navigate({ href: defaultAfterLogin })
    } catch (caught) {
      const apiError = normalizeError(caught)
      setError(apiError)
      return serverFieldErrors(apiError, ['code', 'password'] as const)
    }
  }

  async function handleResend() {
    setNotice(null)
    setError(null)
    try {
      await resend.mutateAsync({ data: { email } })
      setNotice('Te enviamos un código nuevo.')
      countdown.restart(resendCooldownSeconds)
    } catch (caught) {
      setError(normalizeError(caught))
    }
  }

  const alert = error && (
    <div className="flex flex-col gap-2">
      <FormAlert message={errorMessage(error)} />
      {error.code === 'VERIFICATION_PASSWORD_MISMATCH' && (
        <Link to="/recuperar-contrasena" search={{ email }} className="text-sm font-semibold">
          Recupera tu contraseña
        </Link>
      )}
    </div>
  )

  return (
    <>
      <AuthHeader title="Verifica tu correo">
        Escribe el código de 6 dígitos que enviamos a{' '}
        <strong className="font-semibold text-ink">{email}</strong> y tu contraseña.
      </AuthHeader>
      <VerifyEmailForm
        email={email}
        onSubmit={handleSubmit}
        alert={alert}
        resend={
          <div className="flex flex-col gap-1">
            <ResendCodeButton
              secondsLeft={countdown.secondsLeft}
              pending={resend.isPending}
              onResend={handleResend}
            />
            {notice && <output className="text-sm text-success">{notice}</output>}
          </div>
        }
      />
    </>
  )
}
