import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { describe, expect, it } from 'vitest'
import { CodeInput } from './CodeInput'

function Harness({ onValue }: { onValue: (value: string) => void }) {
  const [value, setValue] = useState('')
  return (
    <CodeInput
      label="Código"
      value={value}
      onChange={(next) => {
        setValue(next)
        onValue(next)
      }}
    />
  )
}

describe('CodeInput', () => {
  it('keeps the other digits in place when one in the middle is deleted', async () => {
    const user = userEvent.setup()
    let last = ''
    render(<Harness onValue={(value) => (last = value)} />)

    await user.click(screen.getByLabelText('Dígito 1 de 6'))
    await user.keyboard('123456')
    await user.click(screen.getByLabelText('Dígito 3 de 6'))
    await user.keyboard('{Backspace}')

    expect(screen.getByLabelText('Dígito 3 de 6')).toHaveValue('')
    expect(screen.getByLabelText('Dígito 4 de 6')).toHaveValue('4')
    expect(screen.getByLabelText('Dígito 6 de 6')).toHaveValue('6')
    expect(last).not.toMatch(/^\d{6}$/)

    await user.keyboard('9')

    expect(last).toBe('129456')
  })

  it('clears a digit when the keyboard deletes without a Backspace key event (Android)', async () => {
    let last = ''
    render(<Harness onValue={(value) => (last = value)} />)
    const user = userEvent.setup()
    await user.click(screen.getByLabelText('Dígito 1 de 6'))
    await user.keyboard('123')

    fireEvent.change(screen.getByLabelText('Dígito 2 de 6'), { target: { value: '' } })

    expect(screen.getByLabelText('Dígito 2 de 6')).toHaveValue('')
    expect(screen.getByLabelText('Dígito 3 de 6')).toHaveValue('3')
    expect(last).toBe('1 3')
  })
})
