/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** OAuth client id for "Continuar con Google"; without it the button is hidden. */
  readonly VITE_GOOGLE_CLIENT_ID?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
