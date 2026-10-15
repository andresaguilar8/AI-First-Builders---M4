import { api } from '../../api/client'
import type { MonthView } from '../../api/types'

export const monthQueryKey = (period: string) => ['month', period] as const

export function fetchMonth(period: string) {
  return api.get<MonthView>(`/api/periods/${period}`)
}
