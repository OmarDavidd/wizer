import type { ReactNode } from 'react'

export function AuthHeader({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="flex flex-col gap-2">
      <h1 className="font-display text-[34px] font-bold tracking-[-0.01em]">{title}</h1>
      {children && <p className="text-[15px] text-muted">{children}</p>}
    </div>
  )
}
