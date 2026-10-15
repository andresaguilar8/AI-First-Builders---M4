import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { api, ValidationError } from '../../api/client'
import type {
  CreateExpenseRequest,
  EditScope,
  ExpenseKind,
  ExpenseValues,
  FieldErrors,
  MonthExpense,
  UpdateExpenseRequest,
} from '../../api/types'
import { parseArsAmount, toArsInput } from '../../lib/amount'
import { categoriesQueryKey, fetchCategories } from '../categories/categoryQuery'
import { invalidateAfterChange } from '../month/monthQuery'
import { EditScopeDialog } from './EditScopeDialog'

const NAME_MAX_LENGTH = 100
const DESCRIPTION_MAX_LENGTH = 500

type SaveRequest =
  { mode: 'create'; body: CreateExpenseRequest } | { mode: 'update'; body: UpdateExpenseRequest }

interface ExpenseFormProps {
  /** Mes visualizado: el gasto se crea o se edita en él (RF-02, RF-09). */
  period: string
  /** Gasto a editar; sin él, el formulario da de alta uno nuevo. */
  expense?: MonthExpense
  onDone: () => void
}

/**
 * Alta de un gasto puntual o recurrente en el mes visualizado (RF-01 a RF-05) y edición de un gasto
 * del mes; un recurrente pide antes el alcance del cambio (RF-09, RF-10).
 */
export function ExpenseForm({ period, expense, onDone }: ExpenseFormProps) {
  const queryClient = useQueryClient()
  const [name, setName] = useState(expense?.name ?? '')
  const [amount, setAmount] = useState(expense ? toArsInput(expense.amount) : '')
  const [description, setDescription] = useState(expense?.description ?? '')
  const [dueDate, setDueDate] = useState(expense?.dueDate ?? '')
  const [categoryId, setCategoryId] = useState(expense?.category?.id ?? '')
  const [kind, setKind] = useState<ExpenseKind>('OneOff')
  const [errors, setErrors] = useState<FieldErrors>({})
  /** Valores listos para guardar un recurrente mientras se elige el alcance. */
  const [pendingScope, setPendingScope] = useState<ExpenseValues | null>(null)

  const categories = useQuery({ queryKey: categoriesQueryKey, queryFn: fetchCategories })

  const save = useMutation({
    mutationFn: (request: SaveRequest) =>
      request.mode === 'create'
        ? api.post<MonthExpense>(`/api/periods/${period}/expenses`, request.body)
        : api.put<MonthExpense>(`/api/periods/${period}/expenses/${expense?.id}`, request.body),
    onSuccess: async () => {
      await invalidateAfterChange(queryClient, period)
      onDone()
    },
    onError: (error) => {
      if (error instanceof ValidationError) setErrors(error.errors)
    },
  })

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const localErrors: FieldErrors = {}
    const trimmedName = name.trim()
    const parsedAmount = parseArsAmount(amount)

    if (!trimmedName) localErrors.name = ['El nombre es obligatorio.']
    else if (trimmedName.length > NAME_MAX_LENGTH)
      localErrors.name = [`El nombre admite hasta ${NAME_MAX_LENGTH} caracteres.`]
    if (!amount.trim()) localErrors.amount = ['El monto es obligatorio.']
    else if (!parsedAmount)
      localErrors.amount = ['Ingresá un monto mayor a 0, con hasta 2 decimales (ej.: 1.234,56).']
    if (description.trim().length > DESCRIPTION_MAX_LENGTH)
      localErrors.description = [
        `La descripción admite hasta ${DESCRIPTION_MAX_LENGTH} caracteres.`,
      ]
    // Las fechas `yyyy-MM-dd` se comparan como texto (RF-05).
    if (dueDate && dueDate < `${period}-01`)
      localErrors.dueDate = ['El vencimiento no puede ser anterior al mes del gasto.']

    setErrors(localErrors)
    if (Object.keys(localErrors).length > 0 || !parsedAmount) return

    const values: ExpenseValues = {
      name: trimmedName,
      amount: parsedAmount,
      description: description.trim() || null,
      categoryId: categoryId || null,
      dueDate: dueDate || null,
    }

    if (!expense) save.mutate({ mode: 'create', body: { ...values, kind } })
    else if (expense.kind === 'Recurring') setPendingScope(values)
    else save.mutate({ mode: 'update', body: values })
  }

  const saveWithScope = (scope: EditScope) => {
    if (!pendingScope) return
    save.mutate({ mode: 'update', body: { ...pendingScope, scope } })
    setPendingScope(null)
  }

  const otherErrors = Object.entries(errors)
    .filter(
      ([field]) => !['name', 'amount', 'description', 'categoryId', 'dueDate'].includes(field),
    )
    .flatMap(([, messages]) => messages)

  return (
    <form className="expense-form" onSubmit={handleSubmit} noValidate>
      <label htmlFor="expense-name">Nombre</label>
      {/* El formulario se abre arriba de la lista: el foco lo trae a la vista en el celular. */}
      <input
        id="expense-name"
        autoFocus
        value={name}
        maxLength={NAME_MAX_LENGTH}
        onChange={(event) => setName(event.target.value)}
      />
      <FieldError messages={errors.name} />

      <label htmlFor="expense-amount">Monto</label>
      <input
        id="expense-amount"
        inputMode="decimal"
        placeholder="1.234,56"
        value={amount}
        onChange={(event) => setAmount(event.target.value)}
      />
      <FieldError messages={errors.amount} />

      <label htmlFor="expense-description">Descripción (opcional)</label>
      <textarea
        id="expense-description"
        rows={2}
        maxLength={DESCRIPTION_MAX_LENGTH}
        value={description}
        onChange={(event) => setDescription(event.target.value)}
      />
      <FieldError messages={errors.description} />

      <label htmlFor="expense-category">Categoría (opcional)</label>
      <select
        id="expense-category"
        value={categoryId}
        onChange={(event) => setCategoryId(event.target.value)}
      >
        <option value="">Sin categoría</option>
        {categories.data?.map((category) => (
          <option key={category.id} value={category.id}>
            {category.name}
          </option>
        ))}
      </select>
      <FieldError messages={errors.categoryId} />

      <label htmlFor="expense-due-date">Vencimiento (opcional)</label>
      <input
        id="expense-due-date"
        type="date"
        min={`${period}-01`}
        value={dueDate}
        onChange={(event) => setDueDate(event.target.value)}
      />
      <FieldError messages={errors.dueDate} />

      {!expense && (
        <fieldset>
          <legend>Tipo</legend>
          <label>
            <input
              type="radio"
              name="expense-kind"
              checked={kind === 'OneOff'}
              onChange={() => setKind('OneOff')}
            />
            Puntual
          </label>
          <label>
            <input
              type="radio"
              name="expense-kind"
              checked={kind === 'Recurring'}
              onChange={() => setKind('Recurring')}
            />
            Recurrente
          </label>
        </fieldset>
      )}

      {otherErrors.length > 0 && <FieldError messages={otherErrors} />}
      {save.isError && !(save.error instanceof ValidationError) && (
        <p role="alert" className="form-error">
          No se pudo guardar el gasto. Probá de nuevo.
        </p>
      )}

      <div className="form-actions">
        <button type="submit" disabled={save.isPending || pendingScope !== null}>
          Guardar
        </button>
        <button type="button" onClick={onDone}>
          Cancelar
        </button>
      </div>

      {pendingScope && (
        <EditScopeDialog onChoose={saveWithScope} onCancel={() => setPendingScope(null)} />
      )}
    </form>
  )
}

function FieldError({ messages }: { messages?: string[] }) {
  if (!messages?.length) return null
  return <p className="form-error">{messages.join(' ')}</p>
}
