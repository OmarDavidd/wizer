import { zodResolver } from '@hookform/resolvers/zod'
import { Link } from '@tanstack/react-router'
import { useForm } from 'react-hook-form'
import { TextField } from '@/components/common/TextField'
import { Button } from '@/components/ui/button'
import { type LoginValues, loginSchema } from '../schemas/authSchemas'
import { FormAlert } from './FormAlert'

export type FieldErrors<T> = Partial<Record<keyof T, string>>

interface LoginFormProps {
  /** Resolves with the field errors the server rejected, if any. */
  onSubmit: (values: LoginValues) => Promise<FieldErrors<LoginValues> | void>
  formError: string | null
}

export function LoginForm({ onSubmit, formError }: LoginFormProps) {
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '', rememberMe: false },
  })

  const submit = handleSubmit(async (values) => {
    const fieldErrors = await onSubmit(values)
    for (const [field, message] of Object.entries(fieldErrors ?? {})) {
      setError(field as keyof LoginValues, { message })
    }
  })

  return (
    <form
      noValidate
      onSubmit={submit}
      className="flex flex-col gap-4"
      data-testid="auth-login-form"
    >
      <FormAlert message={formError} />
      <TextField
        label="Correo"
        type="email"
        autoComplete="email"
        placeholder="tu@universidad.edu"
        error={errors.email?.message}
        {...register('email')}
      />
      <TextField
        label="Contraseña"
        type="password"
        autoComplete="current-password"
        error={errors.password?.message}
        labelAction={
          <Link to="/recuperar-contrasena" className="text-[13px] font-medium">
            ¿Olvidaste tu contraseña?
          </Link>
        }
        {...register('password')}
      />
      <label className="flex min-h-11 items-center gap-3 text-sm text-text-2">
        <input type="checkbox" className="size-5 accent-brand" {...register('rememberMe')} />
        Recordarme
      </label>
      <Button type="submit" variant="dark" size="lg" disabled={isSubmitting}>
        Entrar
      </Button>
    </form>
  )
}
