import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import type { MonthExpense } from '../../api/types'
import { periodOf } from '../../lib/dates'
import { ExpenseForm } from '../expenses/ExpenseForm'
import { DueTodayAlert } from './DueTodayAlert'
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

/** Vista principal: gastos del mes visualizado, alta y edición (RF-01, RF-09, RF-34, RF-35). */
export function MonthPage() {
  const [firstRequest] = useState(browserPeriod)
  const [period, setPeriod] = useState<string | null>(null)
  const [adding, setAdding] = useState(false)
  const [editing, setEditing] = useState<MonthExpense | null>(null)

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

  const closeForm = () => {
    setAdding(false)
    setEditing(null)
  }

  const changePeriod = (next: string) => {
    closeForm()
    setPeriod(next)
  }

  const startEditing = (expense: MonthExpense) => {
    setAdding(false)
    setEditing(expense)
  }

  const failed = initial.isError || month.isError

  return (
    <main className="month">
      <h1>Gastos del mes</h1>
      <DueTodayAlert />
      {period && <MonthNavigator period={period} onChange={changePeriod} />}

      {failed && (
        <p role="alert" className="form-error">
          No se pudieron cargar los gastos. Probá recargar la página.
        </p>
      )}
      {!failed && !month.data && <p>Cargando…</p>}

      {period && month.data && (
        <>
          {adding || editing ? (
            <ExpenseForm
              key={editing?.id ?? 'new'}
              period={period}
              expense={editing ?? undefined}
              onDone={closeForm}
            />
          ) : (
            <button type="button" onClick={() => setAdding(true)}>
              Agregar gasto
            </button>
          )}
          <ExpenseList
            period={period}
            today={month.data.today}
            expenses={month.data.expenses}
            onEdit={startEditing}
          />
        </>
      )}
    </main>
  )
}
