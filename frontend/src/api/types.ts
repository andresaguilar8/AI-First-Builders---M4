// Tipos de contracts/api.yaml. Montos como string con punto decimal, fechas `yyyy-MM-dd` y
// períodos `yyyy-MM`.

export type Amount = string
export type IsoDate = string
export type PeriodKey = string

export type ExpenseKind = 'OneOff' | 'Recurring'

export type EditScope = 'ThisMonth' | 'ThisAndFollowing'

export type DueStatus = 'Pagado' | 'AlDia' | 'ProximoAVencer' | 'VenceHoy' | 'Vencido'

export interface ExpenseValues {
  name: string
  description?: string | null
  amount: Amount
  categoryId?: string | null
  dueDate?: IsoDate | null
}

export interface CreateExpenseRequest extends ExpenseValues {
  kind: ExpenseKind
}

export interface UpdateExpenseRequest extends ExpenseValues {
  /** Obligatorio para recurrentes; se ignora en puntuales. */
  scope?: EditScope
}

export interface ConvertExpenseRequest {
  to: ExpenseKind
}

export interface MarkPaidRequest {
  /** Solo para gastos vencidos; si se omite, se registra hoy. */
  paidOn?: IsoDate
}

export interface CategoryRef {
  id: string
  name: string
}

export interface MonthExpense {
  id: string
  kind: ExpenseKind
  name: string
  description?: string | null
  amount: Amount
  /** `null` = "Sin categoría". */
  category?: CategoryRef | null
  dueDate?: IsoDate | null
  paidOn?: IsoDate | null
  status: DueStatus
}

export interface CategoryTotal {
  categoryId?: string | null
  categoryName: string
  total: Amount
}

export interface MonthView {
  period: PeriodKey
  /** Fecha actual en Buenos Aires (RNF-07). */
  today: IsoDate
  expenses: MonthExpense[]
  total: Amount
  pendingTotal: Amount
  byCategory: CategoryTotal[]
}

export interface DueTodayAlert {
  today: IsoDate
  count: number
}

export interface Category {
  id: string
  name: string
}

export interface CategoryInput {
  name: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface Me {
  email: string
}

/** Errores de validación por campo (problem+json, RFC 9457). */
export type FieldErrors = Record<string, string[]>
