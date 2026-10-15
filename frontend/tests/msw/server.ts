import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'

/**
 * Handlers presentes en todos los tests: la vista del mes siempre consulta la alerta del día, que
 * por defecto no tiene vencimientos. Cada test registra los suyos con server.use(...).
 */
export const server = setupServer(
  http.get('/api/alerts/due-today', () => HttpResponse.json({ today: '2026-10-05', count: 0 })),
)
