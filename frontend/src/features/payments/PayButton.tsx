import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api, ValidationError } from '../../api/client'
import type { MarkPaidRequest, MonthExpense, MonthView } from '../../api/types'
import { invalidateAfterChange, monthQueryKey, setMonthExpense } from '../month/monthQuery'
import { OverduePaymentDialog } from './OverduePaymentDialog'

interface PayButtonProps {
  period: string
  /** Fecha actual del backend: es la fecha de pago de un gasto no vencido (RF-28). */
  today: string
  expense: MonthExpense
}

/** Marca o desmarca el pago de un gasto del mes, sin recargar la página (RF-27 a RF-31). */
export function PayButton({ period, today, expense }: PayButtonProps) {
  const queryClient = useQueryClient()
  const [askingDate, setAskingDate] = useState(false)
  const paymentPath = `/api/periods/${period}/expenses/${expense.id}/payment`

  const pay = useMutation({
    mutationFn: (paidOn?: string) =>
      api.put<MonthExpense>(
        paymentPath,
        paidOn ? ({ paidOn } satisfies MarkPaidRequest) : undefined,
      ),
    // Se muestra pagado enseguida y se revierte si el backend lo rechaza.
    onMutate: async (paidOn) => {
      await queryClient.cancelQueries({ queryKey: monthQueryKey(period) })
      const previous = queryClient.getQueryData<MonthView>(monthQueryKey(period))
      setMonthExpense(queryClient, period, {
        ...expense,
        status: 'Pagado',
        paidOn: paidOn ?? today,
      })
      return { previous }
    },
    onError: (_error, _paidOn, context) => {
      if (context?.previous) queryClient.setQueryData(monthQueryKey(period), context.previous)
    },
    onSuccess: (paid) => setMonthExpense(queryClient, period, paid),
    onSettled: () => invalidateAfterChange(queryClient, period),
  })

  const unpay = useMutation({
    mutationFn: () => api.delete<MonthExpense>(paymentPath),
    onSuccess: (pending) => setMonthExpense(queryClient, period, pending),
    onSettled: () => invalidateAfterChange(queryClient, period),
  })

  const confirmPayment = (paidOn?: string) => {
    setAskingDate(false)
    pay.mutate(paidOn)
  }

  const payError =
    pay.error instanceof ValidationError
      ? Object.values(pay.error.errors).flat().join(' ')
      : pay.isError || unpay.isError
        ? 'No se pudo actualizar el pago. Probá de nuevo.'
        : null

  return (
    <>
      {expense.status === 'Pagado' ? (
        <button
          type="button"
          disabled={unpay.isPending}
          onClick={() => unpay.mutate()}
          aria-label={`Desmarcar pago de ${expense.name}`}
        >
          Desmarcar pago
        </button>
      ) : (
        <button
          type="button"
          disabled={pay.isPending || askingDate}
          onClick={() =>
            expense.status === 'Vencido' ? setAskingDate(true) : pay.mutate(undefined)
          }
          aria-label={`Marcar ${expense.name} como pagado`}
        >
          Marcar pagado
        </button>
      )}
      {askingDate && (
        <OverduePaymentDialog
          today={today}
          onConfirm={confirmPayment}
          onCancel={() => setAskingDate(false)}
        />
      )}
      {payError && <p className="form-error">{payError}</p>}
    </>
  )
}
