import { zodResolver } from '@hookform/resolvers/zod'
import { useId } from 'react'
import { useForm } from 'react-hook-form'
import { TextField } from '@/components/common/TextField'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { cn } from '@/lib/utils'
import type { CampusOption } from '../hooks/useCampuses'
import { type RegisterValues, registerSchema } from '../schemas/authSchemas'
import { FormAlert } from './FormAlert'
import type { FieldErrors } from './LoginForm'

interface RegisterFormProps {
  campuses: CampusOption[] | undefined
  campusesFailed: boolean
  onSubmit: (values: RegisterValues) => Promise<FieldErrors<RegisterValues> | void>
  formError: string | null
}

export function RegisterForm({ campuses, campusesFailed, onSubmit, formError }: RegisterFormProps) {
  const campusId = useId()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { fullName: '', email: '', password: '', campusId: '' },
  })

  const submit = handleSubmit(async (values) => {
    const fieldErrors = await onSubmit(values)
    for (const [field, message] of Object.entries(fieldErrors ?? {})) {
      setError(field as keyof RegisterValues, { message })
    }
  })

  const campusError =
    errors.campusId?.message ??
    (campusesFailed ? 'No pudimos cargar los campus. Recarga la página.' : undefined)

  return (
    <form
      noValidate
      onSubmit={submit}
      className="flex flex-col gap-4"
      data-testid="auth-register-form"
    >
      <FormAlert message={formError} />
      <TextField
        label="Nombre completo"
        autoComplete="name"
        placeholder="Laura Ríos"
        error={errors.fullName?.message}
        {...register('fullName')}
      />
      <div className="flex flex-col gap-2">
        <Label htmlFor={campusId}>Campus</Label>
        <select
          id={campusId}
          aria-invalid={campusError ? true : undefined}
          aria-describedby={campusError ? `${campusId}-error` : undefined}
          disabled={!campuses}
          className={cn(
            'h-12 w-full rounded-lg border-[1.5px] border-border-strong bg-surface px-3.5 text-[15px] text-ink',
            'aria-[invalid=true]:border-danger-strong',
          )}
          {...register('campusId')}
        >
          <option value="">{campuses ? 'Elige tu campus' : 'Cargando campus…'}</option>
          {campuses?.map((campus) => (
            <option key={campus.id} value={campus.id}>
              {campus.name}
            </option>
          ))}
        </select>
        {campusError && (
          <p id={`${campusId}-error`} className="text-[13px] text-danger-strong">
            {campusError}
          </p>
        )}
      </div>
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
        autoComplete="new-password"
        error={errors.password?.message}
        {...register('password')}
      />
      <p className="text-xs text-muted">
        Mínimo 10 caracteres, con letras y números. El registro crea una cuenta de alumno; las
        cuentas docentes las crea un administrador.
      </p>
      <Button type="submit" variant="dark" size="lg" disabled={isSubmitting}>
        Crear cuenta
      </Button>
    </form>
  )
}
