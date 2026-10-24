/** A form-level error, announced to screen readers. */
export function FormAlert({ message }: { message: string | null }) {
  if (!message) return null
  return (
    <p role="alert" className="rounded-lg bg-danger-bg px-4 py-3 text-sm text-danger">
      {message}
    </p>
  )
}
