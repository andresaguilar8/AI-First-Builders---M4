import type { QueryClient } from '@tanstack/react-query'
import { api } from '../../api/client'
import type { DueTodayAlert, MonthExpense, MonthView } from '../../api/types'

export const monthQueryKey = (period: string) => ['month', period] as const

export const dueTodayQueryKey = ['alerts', 'due-today'] as const

export function fetchMonth(period: string) {
  return api.get<MonthView>(`/api/periods/${period}`)
}

export function fetchDueToday() {
  return api.get<DueTodayAlert>('/api/alerts/due-today')
}

/** Reemplaza un gasto en la vista del mes ya cargada, sin esperar a recargarla. */
export function setMonthExpense(queryClient: QueryClient, period: string, expense: MonthExpense) {
  queryClient.setQueryData<MonthView>(
    monthQueryKey(period),
    (month) =>
      month && {
        ...month,
        expenses: month.expenses.map((e) => (e.id === expense.id ? expense : e)),
      },
  )
}

/** Tras un cambio en un gasto: recarga el mes (totales incluidos) y la alerta del día. */
export function invalidateAfterChange(queryClient: QueryClient, period: string) {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: monthQueryKey(period) }),
    queryClient.invalidateQueries({ queryKey: dueTodayQueryKey }),
  ])
}
