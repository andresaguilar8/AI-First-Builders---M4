import { http, HttpResponse } from 'msw'
import type { MonthExpense, MonthView } from '../../src/api/types'

export const withSession = http.get('/api/auth/me', () =>
  HttpResponse.json({ email: 'andres@test.local' }),
)

/** Gasto del mes con valores por defecto, para armar respuestas de la API. */
export function monthExpense(overrides: Partial<MonthExpense> = {}): MonthExpense {
  return {
    id: crypto.randomUUID(),
    kind: 'OneOff',
    name: 'Regalo',
    description: null,
    amount: '15000.00',
    category: null,
    dueDate: null,
    paidOn: null,
    status: 'AlDia',
    ...overrides,
  }
}

/**
 * Responde `GET /api/periods/:period` con la fecha `today` dada y los gastos de cada período, y
 * registra los períodos pedidos.
 */
export function monthHandler(today: string, expensesByPeriod: Record<string, MonthExpense[]> = {}) {
  const requested: string[] = []
  const handler = http.get('/api/periods/:period', ({ params }) => {
    const period = String(params.period)
    requested.push(period)
    const view: MonthView = {
      period,
      today,
      expenses: expensesByPeriod[period] ?? [],
      total: '0.00',
      pendingTotal: '0.00',
      byCategory: [],
    }
    return HttpResponse.json(view)
  })
  return { handler, requested }
}
