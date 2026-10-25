import { type ComponentProps, type ReactNode, useId } from 'react'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

interface TextFieldProps extends Omit<ComponentProps<'input'>, 'id'> {
  label: string
  error?: string
  /** Shown at the right of the label, e.g. "¿Olvidaste tu contraseña?". */
  labelAction?: ReactNode
}

/** Label + input + error message, wired with `aria-invalid` and `aria-describedby`. */
export function TextField({ label, error, labelAction, ...inputProps }: TextFieldProps) {
  const id = useId()
  const errorId = `${id}-error`

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-baseline justify-between gap-3">
        <Label htmlFor={id}>{label}</Label>
        {labelAction}
      </div>
      <Input
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
        {...inputProps}
      />
      {error && (
        <p id={errorId} className="text-[13px] text-danger-strong">
          {error}
        </p>
      )}
    </div>
  )
}
