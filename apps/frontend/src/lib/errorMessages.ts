import type { ApiError } from '@/api/errors'

/** User-facing text per error code (.ia/errors.md). A new code is added here in the same PR. */
const byCode: Record<string, string> = {
  INVALID_CREDENTIALS: 'El correo o la contraseña no son correctos.',
  EMAIL_NOT_VERIFIED: 'Tu correo todavía no está verificado. Te enviamos un código nuevo.',
  ACCOUNT_INACTIVE: 'Tu cuenta está desactivada. Escribe a la administración de tu institución.',
  TOO_MANY_LOGIN_ATTEMPTS:
    'Demasiados intentos fallidos. Espera unos minutos y vuelve a intentarlo.',
  INVALID_VERIFICATION_CODE: 'El código no es válido o ya venció. Pide uno nuevo.',
  VERIFICATION_PASSWORD_MISMATCH:
    'El código es correcto, pero la contraseña no coincide con la de esta cuenta.',
  INVALID_RESET_CODE: 'El código no es válido o ya venció. Pide uno nuevo.',
  INVALID_OR_EXPIRED_TOKEN:
    'El enlace no es válido, venció o ya se usó. Pide a la administración que te reenvíe la invitación.',
  EMAIL_ALREADY_EXISTS: 'Ya existe una cuenta con este correo. Inicia sesión con tu contraseña.',
  GOOGLE_TOKEN_INVALID: 'No pudimos validar tu cuenta de Google. Inténtalo de nuevo.',
  CAMPUS_REQUIRED: 'Elige tu campus para terminar de crear la cuenta.',
  RATE_LIMITED: 'Hiciste demasiados pedidos seguidos. Espera unos minutos y vuelve a intentarlo.',
}

const byStatus: Record<number, string> = {
  400: 'Revisa los datos e inténtalo de nuevo.',
  401: 'Tu sesión terminó. Vuelve a iniciar sesión.',
  403: 'No tienes permiso para hacer esto.',
  404: 'No encontramos lo que buscas.',
  409: 'Hubo un conflicto con los datos actuales. Recarga e inténtalo de nuevo.',
  429: 'Hiciste demasiados pedidos seguidos. Espera unos minutos y vuelve a intentarlo.',
}

const serverError = 'Algo salió mal de nuestro lado. Inténtalo de nuevo en unos minutos.'

export function errorMessage(error: ApiError): string {
  if (error.kind === 'network')
    return 'No pudimos conectarnos. Revisa tu conexión e inténtalo de nuevo.'

  const mapped = (error.code && byCode[error.code]) || (error.status && byStatus[error.status])
  if (mapped) return mapped

  return error.correlationId
    ? `${serverError} Código de soporte: ${error.correlationId}`
    : serverError
}
