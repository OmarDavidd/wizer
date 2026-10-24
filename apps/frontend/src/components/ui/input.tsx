import type { ComponentProps } from 'react'
import { cn } from '@/lib/utils'

// .ia/design/components.md §Inputs: 48px, radius 12px, 1.5px border, Inter 15px; danger border on error.
export function Input({ className, ...props }: ComponentProps<'input'>) {
  return (
    <input
      className={cn(
        'h-12 w-full rounded-lg border-[1.5px] border-border-strong bg-surface px-3.5 text-[15px] text-ink placeholder:text-subtle',
        'aria-[invalid=true]:border-danger-strong',
        className,
      )}
      {...props}
    />
  )
}
