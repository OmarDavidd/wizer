import { zodResolver } from '@hookform/resolvers/zod'
import type { ReactNode } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { CodeInput } from '@/components/common/CodeInput'
import { TextField } from '@/components/common/TextField'
import { Button } from '@/components/ui/button'
import { type ResetPasswordValues, resetPasswordSchema } from '../schemas/authSchemas'
import { FormAlert } from './FormAlert'
import type { FieldErrors } from './LoginForm'

interface ResetPasswordFormProps {
  email: string
  onSubmit: (values: ResetPasswordValues) => Promise<FieldErrors<ResetPasswordValues> | void>
  formError: string | null
  resend: ReactNode
}

export function ResetPasswordForm({ email, onSubmit, formError, resend }: ResetPasswordFormProps) {
  const {
    control,
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { email, code: '', newPassword: '' },
  })

  const submit = handleSubmit(async (values) => {
    const fieldErrors = await onSubmit(values)
    for (const [field, message] of Object.entries(fieldErrors ?? {})) {
      setError(field as keyof ResetPasswordValues, { message })
    }
  })

  return (
    <form
      noValidate
      onSubmit={submit}
      className="flex flex-col gap-4"
      data-testid="auth-reset-form"
    >
      <FormAlert message={formError} />
      <Controller
        control={control}
        name="code"
        render={({ field }) => (
          <CodeInput
            label="Código"
            value={field.value}
            onChange={field.onChange}
            error={errors.code?.message}
          />
        )}
      />
      {resend}
      <TextField
        label="Nueva contraseña"
        type="password"
        autoComplete="new-password"
        error={errors.newPassword?.message}
        {...register('newPassword')}
      />
      <p className="text-xs text-muted">Mínimo 10 caracteres, con letras y números.</p>
      <Button type="submit" variant="dark" size="lg" disabled={isSubmitting}>
        Cambiar contraseña
      </Button>
    </form>
  )
}
