import { Link } from '@tanstack/react-router'
import { useState } from 'react'
import { normalizeError } from '@/api/errors'
import {
  usePostApiV1AuthForgotPassword,
  usePostApiV1AuthResetPassword,
} from '@/api/generated/endpoints/auth/auth'
import { useCountdown } from '@/hooks/useCountdown'
import { errorMessage } from '@/lib/errorMessages'
import { AuthHeader } from '../components/AuthHeader'
import { ForgotPasswordForm } from '../components/ForgotPasswordForm'
import { ResendCodeButton } from '../components/ResendCodeButton'
import { ResetPasswordForm } from '../components/ResetPasswordForm'
import type { ForgotPasswordValues, ResetPasswordValues } from '../schemas/authSchemas'
import { serverFieldErrors } from '../utils/fieldErrors'

const resendCooldownSeconds = 60

type Step = { name: 'email' } | { name: 'reset'; email: string } | { name: 'done' }

/** Recovery by code (.ia/authentication.md): ask for a code, then set a new password. It does not sign in. */
export function RecoverPasswordContainer({ email }: { email?: string }) {
  const [step, setStep] = useState<Step>({ name: 'email' })
  const [formError, setFormError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const forgot = usePostApiV1AuthForgotPassword()
  const reset = usePostApiV1AuthResetPassword()
  const countdown = useCountdown(0)

  async function sendCode(values: ForgotPasswordValues) {
    setFormError(null)
    try {
      // Always 202, whether or not the account exists.
      await forgot.mutateAsync({ data: { email: values.email.trim() } })
      countdown.restart(resendCooldownSeconds)
      setStep({ name: 'reset', email: values.email.trim() })
    } catch (error) {
      setFormError(errorMessage(normalizeError(error)))
    }
  }

  async function resend(email: string) {
    setNotice(null)
    try {
      await forgot.mutateAsync({ data: { email } })
      countdown.restart(resendCooldownSeconds)
      setNotice('Te enviamos un código nuevo.')
    } catch (error) {
      setFormError(errorMessage(normalizeError(error)))
    }
  }

  async function changePassword(values: ResetPasswordValues) {
    setFormError(null)
    try {
      await reset.mutateAsync({ data: values })
      setStep({ name: 'done' })
    } catch (error) {
      const apiError = normalizeError(error)
      setFormError(errorMessage(apiError))
      return serverFieldErrors(apiError, ['code', 'newPassword'] as const)
    }
  }

  if (step.name === 'done') {
    return (
      <>
        <AuthHeader title="Listo, tu contraseña cambió">
          Cerramos tus sesiones abiertas. Inicia sesión con tu nueva contraseña.
        </AuthHeader>
        <Link
          to="/ingresar"
          className="flex h-[52px] items-center justify-center rounded-lg bg-ink text-[15px] font-semibold text-white no-underline hover:bg-text-2 hover:text-white"
        >
          Iniciar sesión
        </Link>
      </>
    )
  }

  if (step.name === 'reset') {
    return (
      <>
        <AuthHeader title="Crea una nueva contraseña">
          Si hay una cuenta con {step.email}, te enviamos un código de 6 dígitos. Vence en 15
          minutos.
        </AuthHeader>
        <ResetPasswordForm
          email={step.email}
          onSubmit={changePassword}
          formError={formError}
          resend={
            <div className="flex flex-col gap-1">
              <ResendCodeButton
                secondsLeft={countdown.secondsLeft}
                pending={forgot.isPending}
                onResend={() => void resend(step.email)}
              />
              {notice && <output className="text-sm text-success">{notice}</output>}
            </div>
          }
        />
      </>
    )
  }

  return (
    <>
      <AuthHeader title="Recupera tu contraseña">
        Te enviaremos un código a tu correo para crear una nueva.
      </AuthHeader>
      <ForgotPasswordForm defaultEmail={email ?? ''} onSubmit={sendCode} formError={formError} />
      <Link to="/ingresar" className="self-center text-sm font-semibold">
        Volver a iniciar sesión
      </Link>
    </>
  )
}
