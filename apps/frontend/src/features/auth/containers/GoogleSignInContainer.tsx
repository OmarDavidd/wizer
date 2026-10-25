import { useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { type ReactNode, useCallback, useState } from 'react'
import { normalizeError } from '@/api/errors'
import { Button } from '@/components/ui/button'
import {
  getGetApiV1AuthMeQueryKey,
  usePostApiV1AuthGoogle,
} from '@/api/generated/endpoints/auth/auth'
import { errorMessage } from '@/lib/errorMessages'
import { safeNext } from '@/lib/safeNext'
import { AuthHeader } from '../components/AuthHeader'
import { CampusStepForm } from '../components/CampusStepForm'
import { FormAlert } from '../components/FormAlert'
import { GoogleButton } from '../components/GoogleButton'
import { useCampuses } from '../hooks/useCampuses'

/**
 * Wraps an access screen and adds "Continuar con Google" below it (.ia/authentication.md §Login con Google).
 * A new account answers CAMPUS_REQUIRED: the campus step then replaces the whole screen, and the same
 * ID token (valid for an hour) is resent with the chosen campus.
 */
export function GoogleSignInContainer({ next, children }: { next?: string; children: ReactNode }) {
  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const google = usePostApiV1AuthGoogle()
  const [pendingToken, setPendingToken] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [unavailable, setUnavailable] = useState(false)

  const signIn = useCallback(
    async (idToken: string, campusId?: string) => {
      setFormError(null)
      try {
        const me = await google.mutateAsync({
          data: { idToken, rememberMe: false, campusId: campusId ?? null },
        })
        queryClient.setQueryData(getGetApiV1AuthMeQueryKey(), me)
        await navigate({ href: safeNext(next) })
      } catch (error) {
        const apiError = normalizeError(error)
        if (apiError.code === 'CAMPUS_REQUIRED') {
          setPendingToken(idToken)
          return
        }
        setPendingToken(null)
        setFormError(errorMessage(apiError))
      }
    },
    [google, navigate, next, queryClient],
  )

  if (!clientId || unavailable) return children

  if (pendingToken) {
    return (
      <CampusStep
        onSubmit={(campusId) => signIn(pendingToken, campusId)}
        onBack={() => setPendingToken(null)}
      />
    )
  }

  return (
    <>
      {children}
      <div className="flex flex-col gap-4">
        <div className="flex items-center gap-3 text-[13px] text-subtle">
          <span aria-hidden="true" className="h-px flex-1 bg-border-strong" />o
          <span aria-hidden="true" className="h-px flex-1 bg-border-strong" />
        </div>
        <FormAlert message={formError} />
        <GoogleButton
          clientId={clientId}
          onCredential={(idToken) => void signIn(idToken)}
          onUnavailable={() => setUnavailable(true)}
        />
      </div>
    </>
  )
}

function CampusStep({
  onSubmit,
  onBack,
}: {
  onSubmit: (campusId: string) => Promise<void>
  onBack: () => void
}) {
  const campuses = useCampuses()
  return (
    <>
      <AuthHeader title="Un paso más">
        Elige tu campus para terminar de crear tu cuenta de alumno.
      </AuthHeader>
      <CampusStepForm
        campuses={campuses.data}
        campusesFailed={campuses.isError}
        onSubmit={onSubmit}
      />
      <Button variant="ghost" onClick={onBack} className="self-center">
        Volver
      </Button>
    </>
  )
}
