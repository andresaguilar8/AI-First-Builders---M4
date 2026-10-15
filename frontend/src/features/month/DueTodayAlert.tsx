import { useQuery } from '@tanstack/react-query'
import { dueTodayQueryKey, fetchDueToday } from './monthQuery'

/**
 * Alerta de los gastos pendientes que vencen hoy, consultada en cada apertura o recarga de la app
 * (RF-32). Sin vencimientos de hoy no se muestra nada.
 */
export function DueTodayAlert() {
  const alert = useQuery({ queryKey: dueTodayQueryKey, queryFn: fetchDueToday })
  const count = alert.data?.count ?? 0

  if (count === 0) return null

  return (
    <p role="alert" className="due-today-alert">
      {count === 1 ? 'Tenés 1 gasto que vence hoy.' : `Tenés ${count} gastos que vencen hoy.`}
    </p>
  )
}
