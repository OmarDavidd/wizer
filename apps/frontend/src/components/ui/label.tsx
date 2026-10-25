import type { ComponentProps } from 'react'
import { cn } from '@/lib/utils'

/** Visible field label: Inter 600 13px (.ia/design/components.md §Inputs). Always tied to its control. */
export function Label({
  className,
  htmlFor,
  ...props
}: ComponentProps<'label'> & { htmlFor: string }) {
  return (
    <label
      htmlFor={htmlFor}
      className={cn('text-[13px] font-semibold text-ink', className)}
      {...props}
    />
  )
}
