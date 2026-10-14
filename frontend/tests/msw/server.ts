import { setupServer } from 'msw/node'

// Cada test registra sus propios handlers con server.use(...).
export const server = setupServer()
