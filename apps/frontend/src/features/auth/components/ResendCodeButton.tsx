import { Button } from '@/components/ui/button'

interface ResendCodeButtonProps {
  secondsLeft: number
  pending: boolean
  onResend: () => void
}

/** "Reenviar código", disabled for the 60 s the API needs between codes. */
export function ResendCodeButton({ secondsLeft, pending, onResend }: ResendCodeButtonProps) {
  const waiting = secondsLeft > 0
  return (
    <Button
      variant="ghost"
      onClick={onResend}
      disabled={waiting || pending}
      className="self-start px-0 text-brand"
    >
      {waiting ? `Reenviar código en ${secondsLeft} s` : 'Reenviar código'}
    </Button>
  )
}
