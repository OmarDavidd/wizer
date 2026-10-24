import { cva, type VariantProps } from 'class-variance-authority'
import type { ComponentProps } from 'react'
import { cn } from '@/lib/utils'

// .ia/design/components.md §Botones. At most one `brand` or `dark` button per visual block.
const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2.5 rounded-lg font-semibold transition-colors disabled:pointer-events-none disabled:opacity-60',
  {
    variants: {
      variant: {
        brand: 'bg-brand text-white hover:bg-brand-hover',
        dark: 'bg-ink text-white hover:bg-text-2',
        secondary: 'border-[1.5px] border-border-strong bg-surface text-ink hover:bg-surface-muted',
        accent: 'bg-accent text-ink hover:bg-accent-softer',
        ghost: 'bg-transparent text-text-2 hover:bg-chip',
        danger: 'bg-danger-bg text-danger hover:bg-danger-bg/80',
      },
      size: {
        sm: 'h-9 rounded-md px-3 text-sm',
        md: 'h-11 px-4 text-sm',
        lg: 'h-[52px] px-5 text-[15px]',
      },
    },
    defaultVariants: { variant: 'secondary', size: 'md' },
  },
)

export type ButtonProps = ComponentProps<'button'> & VariantProps<typeof buttonVariants>

export function Button({ className, variant, size, type = 'button', ...props }: ButtonProps) {
  return (
    <button type={type} className={cn(buttonVariants({ variant, size }), className)} {...props} />
  )
}
