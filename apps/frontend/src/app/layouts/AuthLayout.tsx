import type { ReactNode } from 'react'
import { institutionName } from '@/app/branding'

// .ia/design/layouts.md §AuthLayout: brand panel cut diagonally; below 860px the cut turns horizontal.
const diagonalCut =
  '[clip-path:polygon(0_0,58%_0,42%_100%,0_100%)] max-[860px]:[clip-path:polygon(0_0,100%_0,100%_380px,0_460px)]'

export function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <div className="relative min-h-screen overflow-hidden bg-ground">
      <div aria-hidden="true" className={`absolute inset-0 bg-brand ${diagonalCut}`} />

      <div className="relative flex min-h-screen flex-wrap">
        <section className="flex flex-[1_1_420px] flex-col justify-between gap-10 px-16 py-14 pr-[9%] text-white max-[860px]:min-h-[380px] max-[860px]:px-6 max-[860px]:py-10">
          <div className="flex items-center gap-3">
            <span
              aria-hidden="true"
              className="flex size-10 items-center justify-center rounded-lg bg-accent font-display text-[22px] font-bold text-ink"
            >
              W
            </span>
            <span className="font-display text-2xl font-bold">Wizer</span>
          </div>
          <div className="flex max-w-[460px] flex-col gap-5">
            <p className="font-display text-[56px] leading-[1.02] font-bold tracking-[-0.02em] max-[860px]:text-[40px]">
              Tu clase semipresencial, en un solo lugar.
            </p>
            <p className="text-[17px] leading-[1.55] text-on-brand-muted">
              Chat, equipos, tareas, pizarra y archivos conectados entre todos los campus.
            </p>
          </div>
          <p className="text-[13px] text-brand-subtle">{institutionName}</p>
        </section>

        <main className="flex flex-[1_1_420px] items-center justify-center px-6 py-12 pl-[9%] max-[860px]:pl-6">
          <div className="flex w-full max-w-[400px] flex-col gap-6">{children}</div>
        </main>
      </div>
    </div>
  )
}
