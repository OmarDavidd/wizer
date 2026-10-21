import { defineConfig } from 'orval'

// The OpenAPI document is produced by the backend build (apps/backend/openapi).
const input = '../backend/openapi/wizer-api.json'

export default defineConfig({
  wizer: {
    input,
    output: {
      mode: 'tags-split',
      target: 'src/api/generated/endpoints',
      schemas: 'src/api/generated/model',
      client: 'react-query',
      httpClient: 'axios',
      clean: true,
      override: {
        mutator: { path: 'src/api/client.ts', name: 'customInstance' },
      },
    },
  },
  wizerZod: {
    input,
    output: {
      mode: 'tags-split',
      target: 'src/api/generated/zod',
      client: 'zod',
      fileExtension: '.zod.ts',
      clean: true,
    },
  },
})
