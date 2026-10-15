import type { DueStatus, MonthExpense } from '../../api/types'
import { formatArs } from '../../lib/amount'
import { formatDate } from '../../lib/dates'
import { PayButton } from '../payments/PayButton'

const statusLabels: Record<Exclude<DueStatus, 'Pagado'>, string> = {
  AlDia: 'Al día',
  ProximoAVencer: 'Próximo a vencer',
  VenceHoy: 'Vence hoy',
  Vencido: 'Vencido',
}

interface ExpenseListProps {
  period: string
  /** Fecha actual del backend (Buenos Aires). */
  today: string
  expenses: MonthExpense[]
  onEdit: (expense: MonthExpense) => void
}

/** Gastos del mes con su monto, categoría, vencimiento, tipo y estado (RF-33, RF-34). */
export function ExpenseList({ period, today, expenses, onEdit }: ExpenseListProps) {
  if (expenses.length === 0) return <p className="empty">No hay gastos en este mes.</p>

  return (
    <ul className="expense-list">
      {expenses.map((expense) => (
        <li
          key={expense.id}
          className={expense.kind === 'Recurring' ? 'expense expense--recurring' : 'expense'}
        >
          <div className="expense-main">
            <strong>{expense.name}</strong>
            <span className="expense-amount">{formatArs(expense.amount)}</span>
          </div>
          <div className="expense-details">
            <span className={`status status--${expense.status}`}>
              {expense.status === 'Pagado'
                ? `Pagado el ${expense.paidOn ? formatDate(expense.paidOn) : ''}`
                : statusLabels[expense.status]}
            </span>
            <span>{expense.category?.name ?? 'Sin categoría'}</span>
            <span>
              {expense.dueDate ? `Vence ${formatDate(expense.dueDate)}` : 'Sin vencimiento'}
            </span>
            <span>{expense.kind === 'Recurring' ? 'Recurrente' : 'Puntual'}</span>
          </div>
          <div className="expense-actions">
            <PayButton period={period} today={today} expense={expense} />
            <button
              type="button"
              onClick={() => onEdit(expense)}
              aria-label={`Editar ${expense.name}`}
            >
              Editar
            </button>
          </div>
        </li>
      ))}
    </ul>
  )
}
