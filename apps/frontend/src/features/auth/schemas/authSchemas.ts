import { z } from 'zod'
import { PostApiV1AuthLoginBody } from '@/api/generated/zod/auth/auth.zod'

// The generated schemas carry the API shape; these add the rules and the Spanish messages (.ia/frontend.md §Formularios).

export const emailField = z
  .email({ error: 'Ingresa un correo válido.' })
  .max(320, { error: 'El correo es demasiado largo.' })

export const loginSchema = PostApiV1AuthLoginBody.extend({
  email: emailField,
  password: z.string().min(1, { error: 'Ingresa tu contraseña.' }),
})

export type LoginValues = z.infer<typeof loginSchema>

/** Same rule as the API (.ia/authentication.md §Registro): 10+ characters, a letter and a number. */
export const newPasswordField = z
  .string()
  .min(10, { error: 'Usa al menos 10 caracteres.' })
  .max(128, { error: 'Usa como máximo 128 caracteres.' })
  .regex(/\p{L}/u, { error: 'Incluye al menos una letra.' })
  .regex(/\p{N}/u, { error: 'Incluye al menos un número.' })

const notTheEmail = (values: { email: string; password: string }) =>
  values.password.trim().toLowerCase() !== values.email.trim().toLowerCase()

export const codeField = z.string().regex(/^\d{6}$/, { error: 'Ingresa los 6 dígitos del código.' })

export const registerSchema = z
  .object({
    fullName: z
      .string()
      .trim()
      .min(1, { error: 'Ingresa tu nombre completo.' })
      .max(120, { error: 'Usa como máximo 120 caracteres.' }),
    email: emailField,
    password: newPasswordField,
    campusId: z.uuid({ error: 'Elige tu campus.' }),
  })
  .refine(notTheEmail, { error: 'La contraseña no puede ser tu correo.', path: ['password'] })

export type RegisterValues = z.infer<typeof registerSchema>

export const verifyEmailSchema = z.object({
  email: emailField,
  code: codeField,
  password: z.string().min(1, { error: 'Ingresa tu contraseña.' }),
})

export type VerifyEmailValues = z.infer<typeof verifyEmailSchema>

export const forgotPasswordSchema = z.object({ email: emailField })

export type ForgotPasswordValues = z.infer<typeof forgotPasswordSchema>

export const resetPasswordSchema = z
  .object({ email: emailField, code: codeField, newPassword: newPasswordField })
  .refine((values) => notTheEmail({ email: values.email, password: values.newPassword }), {
    error: 'La contraseña no puede ser tu correo.',
    path: ['newPassword'],
  })

export type ResetPasswordValues = z.infer<typeof resetPasswordSchema>
