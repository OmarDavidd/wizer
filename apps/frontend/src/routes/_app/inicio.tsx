import { createFileRoute } from '@tanstack/react-router'
import { useMe } from '@/features/auth'

// Placeholder so a sign-in has somewhere to land; the real Inicio screens replace it.
export const Route = createFileRoute('/_app/inicio')({
  component: function InicioPage() {
    const { data: me } = useMe()
    return (
      <main className="p-10">
        <h1 className="font-display text-[40px] leading-[1.1] font-bold tracking-[-0.02em]">
          Hola, {me?.fullName}
        </h1>
      </main>
    )
  },
})
