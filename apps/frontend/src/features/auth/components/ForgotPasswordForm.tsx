import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { TextField } from '@/components/common/TextField'
import { Button } from '@/components/ui/button'
import { type ForgotPasswordValues, forgotPasswordSchema } from '../schemas/authSchemas'
import { FormAlert } from './FormAlert'

interface ForgotPasswordFormProps {
  defaultEmail: string
  onSubmit: (values: ForgotPasswordValues) => Promise<void>
  formError: string | null
}

export function ForgotPasswordForm({ defaultEmail, onSubmit, formError }: ForgotPasswordFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: defaultEmail },
  })

  return (
    <form
      noValidate
      onSubmit={handleSubmit(onSubmit)}
      className="flex flex-col gap-4"
      data-testid="auth-forgot-form"
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
      <Button type="submit" variant="dark" size="lg" disabled={isSubmitting}>
        Enviar código
      </Button>
    </form>
  )
}
