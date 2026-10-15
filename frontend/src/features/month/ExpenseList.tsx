import type { MonthExpense } from '../../api/types'
import { formatArs } from '../../lib/amount'
import { formatDate } from '../../lib/dates'

interface ExpenseListProps {
  expenses: MonthExpense[]
}

/** Gastos del mes con su monto, categoría, vencimiento y tipo (RF-34). */
export function ExpenseList({ expenses }: ExpenseListProps) {
  if (expenses.length === 0) return <p className="empty">No hay gastos en este mes.</p>

  return (
    <ul className="expense-list">
      {expenses.map((expense) => (
        <li key={expense.id} className="expense">
          <div className="expense-main">
            <strong>{expense.name}</strong>
            <span className="expense-amount">{formatArs(expense.amount)}</span>
          </div>
          <div className="expense-details">
            <span>{expense.category?.name ?? 'Sin categoría'}</span>
            <span>
              {expense.dueDate ? `Vence ${formatDate(expense.dueDate)}` : 'Sin vencimiento'}
            </span>
            <span>{expense.kind === 'Recurring' ? 'Recurrente' : 'Puntual'}</span>
          </div>
        </li>
      ))}
    </ul>
  )
}
