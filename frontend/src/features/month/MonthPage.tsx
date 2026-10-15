import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { periodOf } from '../../lib/dates'
import { ExpenseForm } from '../expenses/ExpenseForm'
import { ExpenseList } from './ExpenseList'
import { MonthNavigator } from './MonthNavigator'
import { fetchMonth, monthQueryKey } from './monthQuery'

/**
 * Primer pedido antes de conocer "hoy": el mes del navegador. Es solo una aproximación; el mes
 * que se muestra sale siempre del `today` del backend (RF-34, RNF-07).
 */
function browserPeriod() {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
}

/** Vista principal: gastos del mes visualizado y alta de gastos (RF-01, RF-34, RF-35). */
export function MonthPage() {
  const [firstRequest] = useState(browserPeriod)
  const [period, setPeriod] = useState<string | null>(null)
  const [adding, setAdding] = useState(false)

  const initial = useQuery({
    queryKey: monthQueryKey(firstRequest),
    queryFn: () => fetchMonth(firstRequest),
    enabled: period === null,
  })
  if (period === null && initial.data) setPeriod(periodOf(initial.data.today))

  const month = useQuery({
    queryKey: monthQueryKey(period ?? firstRequest),
    queryFn: () => fetchMonth(period ?? firstRequest),
    enabled: period !== null,
  })

  const changePeriod = (next: string) => {
    setAdding(false)
    setPeriod(next)
  }

  const failed = initial.isError || month.isError

  return (
    <main className="month">
      <h1>Gastos del mes</h1>
      {period && <MonthNavigator period={period} onChange={changePeriod} />}

      {failed && (
        <p role="alert" className="form-error">
          No se pudieron cargar los gastos. Probá recargar la página.
        </p>
      )}
      {!failed && !month.data && <p>Cargando…</p>}

      {period && month.data && (
        <>
          {adding ? (
            <ExpenseForm period={period} onDone={() => setAdding(false)} />
          ) : (
            <button type="button" onClick={() => setAdding(true)}>
              Agregar gasto
            </button>
          )}
          <ExpenseList expenses={month.data.expenses} />
        </>
      )}
    </main>
  )
}
