import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../../api/client'
import type { ConvertExpenseRequest, MonthExpense } from '../../api/types'
import { formatPeriod } from '../../lib/dates'
import { invalidateAfterChange } from '../month/monthQuery'

type Action = 'delete' | 'discontinue' | 'toRecurring' | 'toOneOff'

interface ExpenseActionsProps {
  period: string
  expense: MonthExpense
}

/**
 * Corregir el plan desde el mes visualizado: un puntual se elimina o se hace recurrente; un
 * recurrente se da de baja o se hace puntual. Todo pide confirmación (RF-12 a RF-16).
 */
export function ExpenseActions({ period, expense }: ExpenseActionsProps) {
  const queryClient = useQueryClient()
  const [confirming, setConfirming] = useState<Action | null>(null)
  const expensePath = `/api/periods/${period}/expenses/${expense.id}`
  const month = formatPeriod(period)

  const run = useMutation({
    mutationFn: (action: Action) => {
      switch (action) {
        case 'delete':
          return api.delete<undefined>(expensePath)
        case 'discontinue':
          return api.post<undefined>(`${expensePath}/discontinue`)
        case 'toRecurring':
          return api.post<MonthExpense>(`${expensePath}/convert`, {
            to: 'Recurring',
          } satisfies ConvertExpenseRequest)
        case 'toOneOff':
          return api.post<MonthExpense>(`${expensePath}/convert`, {
            to: 'OneOff',
          } satisfies ConvertExpenseRequest)
      }
    },
    onSettled: () => invalidateAfterChange(queryClient, period),
  })

  const questions: Record<Action, { title: string; detail: string }> = {
    delete: { title: `¿Eliminar «${expense.name}»?`, detail: 'La eliminación es definitiva.' },
    discontinue: {
      title: `¿Dar de baja «${expense.name}» desde ${month}?`,
      detail: 'No aparecerá desde este mes; los meses anteriores no cambian.',
    },
    toRecurring: {
      title: `¿Hacer recurrente «${expense.name}»?`,
      detail: `Se repetirá todos los meses desde ${month} con sus valores actuales.`,
    },
    toOneOff: {
      title: `¿Hacer puntual «${expense.name}»?`,
      detail: `Quedará solo en ${month}; los meses anteriores no cambian.`,
    },
  }

  const confirm = () => {
    if (confirming) run.mutate(confirming)
    setConfirming(null)
  }

  const actions: [Action, string][] =
    expense.kind === 'OneOff'
      ? [
          ['delete', 'Eliminar'],
          ['toRecurring', 'Hacer recurrente'],
        ]
      : [
          ['discontinue', 'Dar de baja desde este mes'],
          ['toOneOff', 'Hacer puntual'],
        ]

  return (
    <>
      {actions.map(([action, label]) => (
        <button
          key={action}
          type="button"
          disabled={run.isPending}
          onClick={() => setConfirming(action)}
          aria-label={`${label} ${expense.name}`}
        >
          {label}
        </button>
      ))}
      {confirming && (
        <div
          className="dialog"
          role="dialog"
          aria-modal="true"
          aria-labelledby={`confirm-${expense.id}`}
        >
          <h3 id={`confirm-${expense.id}`}>{questions[confirming].title}</h3>
          <p>{questions[confirming].detail}</p>
          <div className="dialog-actions">
            <button type="button" onClick={confirm}>
              Confirmar
            </button>
            <button type="button" onClick={() => setConfirming(null)}>
              Cancelar
            </button>
          </div>
        </div>
      )}
      {run.isError && (
        <p role="alert" className="form-error">
          No se pudo completar la acción. Probá de nuevo.
        </p>
      )}
    </>
  )
}
