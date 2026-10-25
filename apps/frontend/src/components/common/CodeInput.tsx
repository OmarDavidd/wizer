import { type ClipboardEvent, type KeyboardEvent, useId, useRef } from 'react'
import { cn } from '@/lib/utils'

interface CodeInputProps {
  label: string
  value: string
  onChange: (value: string) => void
  length?: number
  error?: string
}

/**
 * One box per digit (.ia/design/pages.md: "6 casillas para el código"). Typing moves forward, Backspace
 * clears or moves back, and pasting a whole code fills every box.
 */
export function CodeInput({ label, value, onChange, length = 6, error }: CodeInputProps) {
  const id = useId()
  const inputs = useRef<(HTMLInputElement | null)[]>([])
  // Each box keeps its position: an empty box is a space in `value`, so deleting one never shifts the
  // others. The value is only a valid code once it is all digits.
  const digits = Array.from({ length }, (_, index) => (value[index] ?? '').trim())

  function setDigits(next: string[], focusIndex: number) {
    onChange(
      next
        .map((digit) => digit || ' ')
        .join('')
        .trimEnd(),
    )
    inputs.current[Math.min(focusIndex, length - 1)]?.focus()
  }

  function handleInput(index: number, raw: string) {
    // Some mobile keyboards (Android) delete without a Backspace key event: an emptied box is cleared here.
    if (raw === '') {
      const next = [...digits]
      next[index] = ''
      setDigits(next, index)
      return
    }
    const typed = raw.replace(/\D/g, '')
    if (!typed) return
    const next = [...digits]
    for (const [offset, digit] of [...typed].entries()) {
      if (index + offset < length) next[index + offset] = digit
    }
    setDigits(next, index + typed.length)
  }

  function handleKeyDown(index: number, event: KeyboardEvent<HTMLInputElement>) {
    if (event.key !== 'Backspace') return
    event.preventDefault()
    const next = [...digits]
    if (next[index]) {
      next[index] = ''
      setDigits(next, index)
    } else if (index > 0) {
      next[index - 1] = ''
      setDigits(next, index - 1)
    }
  }

  function handlePaste(index: number, event: ClipboardEvent<HTMLInputElement>) {
    event.preventDefault()
    handleInput(index, event.clipboardData.getData('text'))
  }

  return (
    <fieldset className="flex flex-col gap-2" aria-describedby={error ? `${id}-error` : undefined}>
      <legend className="mb-2 text-[13px] font-semibold text-ink">{label}</legend>
      <div className="flex gap-2">
        {digits.map((digit, index) => (
          <input
            key={index}
            ref={(element) => {
              inputs.current[index] = element
            }}
            aria-label={`Dígito ${index + 1} de ${length}`}
            aria-invalid={error ? true : undefined}
            inputMode="numeric"
            autoComplete={index === 0 ? 'one-time-code' : 'off'}
            maxLength={1}
            value={digit}
            onChange={(event) => handleInput(index, event.target.value)}
            onKeyDown={(event) => handleKeyDown(index, event)}
            onPaste={(event) => handlePaste(index, event)}
            className={cn(
              'h-14 w-full min-w-0 rounded-lg border-[1.5px] border-border-strong bg-surface text-center font-display text-2xl font-bold text-ink',
              'aria-[invalid=true]:border-danger-strong',
            )}
          />
        ))}
      </div>
      {error && (
        <p id={`${id}-error`} className="text-[13px] text-danger-strong">
          {error}
        </p>
      )}
    </fieldset>
  )
}
