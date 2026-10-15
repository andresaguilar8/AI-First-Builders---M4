import { http, HttpResponse } from 'msw'
import type { Category, CategoryInput, MonthExpense, MonthView } from '../../src/api/types'

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

export function alertHandler(count: number, today = '2026-10-05') {
  return http.get('/api/alerts/due-today', () => HttpResponse.json({ today, count }))
}

interface PaymentCall {
  method: 'PUT' | 'DELETE'
  body?: { paidOn?: string }
}

/**
 * Marca y desmarca pagos sobre los gastos dados (como hace el backend) y registra las llamadas.
 * Al desmarcar, el gasto vuelve al estado que tenía antes de pagarse.
 */
export function paymentHandlers(expenses: Record<string, MonthExpense[]>, today: string) {
  const calls: PaymentCall[] = []
  const pendingStatus = new Map<string, MonthExpense['status']>()
  const update = (period: string, id: string, change: (e: MonthExpense) => MonthExpense) => {
    expenses[period] = expenses[period].map((e) => (e.id === id ? change(e) : e))
    return expenses[period].find((e) => e.id === id) as MonthExpense
  }
  const path = '/api/periods/:period/expenses/:id/payment'
  const handlers = [
    http.put(path, async ({ params, request }) => {
      const text = await request.text()
      const body = text ? (JSON.parse(text) as { paidOn?: string }) : undefined
      calls.push({ method: 'PUT', body })
      const paid = update(String(params.period), String(params.id), (e) => {
        pendingStatus.set(e.id, e.status)
        return { ...e, status: 'Pagado', paidOn: body?.paidOn ?? today }
      })
      return HttpResponse.json(paid)
    }),
    http.delete(path, ({ params }) => {
      calls.push({ method: 'DELETE' })
      const pending = update(String(params.period), String(params.id), (e) => ({
        ...e,
        status: pendingStatus.get(e.id) ?? 'AlDia',
        paidOn: null,
      }))
      return HttpResponse.json(pending)
    }),
  ]
  return { handlers, calls }
}

/**
 * ABM de categorías en memoria. Los nombres de `taken` (además de los existentes) y "Sin categoría"
 * se rechazan con 409, comparados sin mayúsculas ni acentos como en el backend.
 */
export function categoryHandlers(initial: Category[] = []) {
  let categories = [...initial]
  const normalize = (name: string) =>
    name.normalize('NFD').replace(/\p{M}/gu, '').trim().replace(/\s+/g, ' ').toLowerCase()
  const conflicts = (name: string, exceptId?: string) =>
    normalize(name) === 'sin categoria' ||
    categories.some((c) => c.id !== exceptId && normalize(c.name) === normalize(name))
  const handlers = [
    http.get('/api/categories', () => HttpResponse.json(categories)),
    http.post('/api/categories', async ({ request }) => {
      const { name } = (await request.json()) as CategoryInput
      if (conflicts(name)) return new HttpResponse(null, { status: 409 })
      const created = { id: crypto.randomUUID(), name: name.trim() }
      categories = [...categories, created]
      return HttpResponse.json(created, { status: 201 })
    }),
    http.put('/api/categories/:id', async ({ params, request }) => {
      const { name } = (await request.json()) as CategoryInput
      if (conflicts(name, String(params.id))) return new HttpResponse(null, { status: 409 })
      categories = categories.map((c) => (c.id === params.id ? { ...c, name: name.trim() } : c))
      return HttpResponse.json(categories.find((c) => c.id === params.id))
    }),
    http.delete('/api/categories/:id', ({ params }) => {
      categories = categories.filter((c) => c.id !== params.id)
      return new HttpResponse(null, { status: 204 })
    }),
  ]
  return { handlers, current: () => categories }
}
