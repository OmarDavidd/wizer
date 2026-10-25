import { zodResolver } from '@hookform/resolvers/zod'
import { useId } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { cn } from '@/lib/utils'
import type { CampusOption } from '../hooks/useCampuses'

const campusStepSchema = z.object({ campusId: z.uuid({ error: 'Elige tu campus.' }) })

type CampusStepValues = z.infer<typeof campusStepSchema>

interface CampusStepFormProps {
  campuses: CampusOption[] | undefined
  campusesFailed: boolean
  onSubmit: (campusId: string) => Promise<void>
}

/** First Google sign-in: the account is created in the campus chosen here (BR-ACC-05). */
export function CampusStepForm({ campuses, campusesFailed, onSubmit }: CampusStepFormProps) {
  const id = useId()
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CampusStepValues>({
    resolver: zodResolver(campusStepSchema),
    defaultValues: { campusId: '' },
  })
  const error =
    errors.campusId?.message ??
    (campusesFailed ? 'No pudimos cargar los campus. Recarga la página.' : undefined)

  return (
    <form
      noValidate
      onSubmit={handleSubmit((values) => onSubmit(values.campusId))}
      className="flex flex-col gap-4"
      data-testid="auth-google-campus-form"
    >
      <div className="flex flex-col gap-2">
        <Label htmlFor={id}>Campus</Label>
        <select
          id={id}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? `${id}-error` : undefined}
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
        {error && (
          <p id={`${id}-error`} className="text-[13px] text-danger-strong">
            {error}
          </p>
        )}
      </div>
      <Button type="submit" variant="dark" size="lg" disabled={isSubmitting}>
        Continuar
      </Button>
    </form>
  )
}
