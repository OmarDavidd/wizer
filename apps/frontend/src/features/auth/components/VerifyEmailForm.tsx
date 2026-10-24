import { zodResolver } from '@hookform/resolvers/zod'
import type { ReactNode } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { CodeInput } from '@/components/common/CodeInput'
import { TextField } from '@/components/common/TextField'
import { Button } from '@/components/ui/button'
import { type VerifyEmailValues, verifyEmailSchema } from '../schemas/authSchemas'
import type { FieldErrors } from './LoginForm'

interface VerifyEmailFormProps {
  email: string
  onSubmit: (values: VerifyEmailValues) => Promise<FieldErrors<VerifyEmailValues> | void>
  /** Form-level error (with its own actions, such as a recovery link). */
  alert: ReactNode
  resend: ReactNode
}

export function VerifyEmailForm({ email, onSubmit, alert, resend }: VerifyEmailFormProps) {
  const {
    control,
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<VerifyEmailValues>({
    resolver: zodResolver(verifyEmailSchema),
    defaultValues: { email, code: '', password: '' },
  })

  const submit = handleSubmit(async (values) => {
    const fieldErrors = await onSubmit(values)
    for (const [field, message] of Object.entries(fieldErrors ?? {})) {
      setError(field as keyof VerifyEmailValues, { message })
    }
  })

  return (
    <form
      noValidate
      onSubmit={submit}
      className="flex flex-col gap-4"
      data-testid="auth-verify-form"
    >
      {alert}
      <Controller
        control={control}
        name="code"
        render={({ field }) => (
          <CodeInput
            label="Código de verificación"
            value={field.value}
            onChange={field.onChange}
            error={errors.code?.message}
          />
        )}
      />
      {resend}
      <TextField
        label="Contraseña"
        type="password"
        autoComplete="current-password"
        error={errors.password?.message}
        {...register('password')}
      />
      <Button type="submit" variant="dark" size="lg" disabled={isSubmitting}>
        Verificar y entrar
      </Button>
    </form>
  )
}
