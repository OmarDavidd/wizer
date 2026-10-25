import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { TextField } from '@/components/common/TextField'
import { Button } from '@/components/ui/button'
import { type ActivateAccountValues, activateAccountSchema } from '../schemas/authSchemas'
import { FormAlert } from './FormAlert'
import type { FieldErrors } from './LoginForm'

interface ActivateAccountFormProps {
  onSubmit: (values: ActivateAccountValues) => Promise<FieldErrors<ActivateAccountValues> | void>
  formError: string | null
}

export function ActivateAccountForm({ onSubmit, formError }: ActivateAccountFormProps) {
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ActivateAccountValues>({
    resolver: zodResolver(activateAccountSchema),
    defaultValues: { password: '' },
  })

  const submit = handleSubmit(async (values) => {
    const fieldErrors = await onSubmit(values)
    for (const [field, message] of Object.entries(fieldErrors ?? {})) {
      setError(field as keyof ActivateAccountValues, { message })
    }
  })

  return (
    <form
      noValidate
      onSubmit={submit}
      className="flex flex-col gap-4"
      data-testid="auth-activate-form"
    >
      <FormAlert message={formError} />
      <TextField
        label="Contraseña"
        type="password"
        autoComplete="new-password"
        error={errors.password?.message}
        {...register('password')}
      />
      <p className="text-xs text-muted">Mínimo 10 caracteres, con letras y números.</p>
      <Button type="submit" variant="dark" size="lg" disabled={isSubmitting}>
        Crear contraseña y entrar
      </Button>
    </form>
  )
}
