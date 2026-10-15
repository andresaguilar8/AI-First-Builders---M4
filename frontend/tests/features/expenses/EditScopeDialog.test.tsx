import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import type { MonthExpense, UpdateExpenseRequest } from '../../../src/api/types'
import { server } from '../../msw/server'
import { monthExpense, monthHandler, withSession } from '../../msw/month'
import { renderApp } from '../../renderApp'

/** Registra los PUT de edición y actualiza el gasto en el mes. */
function updateHandler(expenses: Record<string, MonthExpense[]>) {
  const updates: UpdateExpenseRequest[] = []
  const handler = http.put('/api/periods/:period/expenses/:id', async ({ params, request }) => {
    const body = (await request.json()) as UpdateExpenseRequest
    updates.push(body)
    const period = String(params.period)
    const current = expenses[period].find((e) => e.id === params.id) as MonthExpense
    const updated = { ...current, ...body }
    expenses[period] = expenses[period].map((e) => (e.id === updated.id ? updated : e))
    return HttpResponse.json(updated)
  })
  return { handler, updates }
}

async function setup(expense: MonthExpense) {
  const expenses = { '2026-10': [expense] }
  const update = updateHandler(expenses)
  server.use(withSession, monthHandler('2026-10-05', expenses).handler, update.handler)
  renderApp('/')
  const user = userEvent.setup()
  await user.click(await screen.findByRole('button', { name: `Editar ${expense.name}` }))
  return { user, update }
}

async function changeAmount(user: ReturnType<typeof userEvent.setup>, amount: string) {
  const input = screen.getByLabelText('Monto')
  await user.clear(input)
  await user.type(input, amount)
  await user.click(screen.getByRole('button', { name: 'Guardar' }))
}

const rent = monthExpense({
  name: 'Alquiler',
  kind: 'Recurring',
  amount: '30000.00',
  description: 'Depto',
  dueDate: '2026-10-10',
})

describe('EditScopeDialog', () => {
  it('el formulario de edición muestra los valores actuales del gasto', async () => {
    await setup(rent)

    expect(screen.getByLabelText('Nombre')).toHaveValue('Alquiler')
    expect(screen.getByLabelText('Monto')).toHaveValue('30.000')
    expect(screen.getByLabelText('Descripción (opcional)')).toHaveValue('Depto')
    expect(screen.getByLabelText('Vencimiento (opcional)')).toHaveValue('2026-10-10')
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('editar un recurrente exige elegir "Solo este mes" antes de guardar', async () => {
    const { user, update } = await setup(rent)

    await changeAmount(user, '35.000')

    const dialog = await screen.findByRole('dialog', { name: '¿A qué meses aplicar el cambio?' })
    expect(update.updates).toHaveLength(0)
    await user.click(within(dialog).getByRole('button', { name: 'Solo este mes' }))

    expect(await screen.findByText('$ 35.000,00')).toBeInTheDocument()
    expect(update.updates).toEqual([
      {
        name: 'Alquiler',
        amount: '35000.00',
        description: 'Depto',
        categoryId: null,
        dueDate: '2026-10-10',
        scope: 'ThisMonth',
      },
    ])
  })

  it('editar un recurrente permite elegir "Este mes y los siguientes"', async () => {
    const { user, update } = await setup(rent)

    await changeAmount(user, '35.000')
    await user.click(await screen.findByRole('button', { name: 'Este mes y los siguientes' }))

    await screen.findByText('$ 35.000,00')
    expect(update.updates[0].scope).toBe('ThisAndFollowing')
  })

  it('cancelar la elección del alcance no guarda y vuelve al formulario', async () => {
    const { user, update } = await setup(rent)

    await changeAmount(user, '35.000')
    await user.click(await screen.findByRole('button', { name: 'Volver' }))

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Monto')).toHaveValue('35.000')
    expect(update.updates).toHaveLength(0)
  })

  it('editar un puntual no pide alcance', async () => {
    const { user, update } = await setup(monthExpense({ name: 'Regalo', amount: '15000.00' }))

    await changeAmount(user, '18.000')

    expect(await screen.findByText('$ 18.000,00')).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(update.updates).toHaveLength(1)
    expect(update.updates[0]).not.toHaveProperty('scope')
  })

  it('los recurrentes se distinguen visualmente en la lista', async () => {
    server.use(
      withSession,
      monthHandler('2026-10-05', { '2026-10': [rent, monthExpense({ name: 'Regalo' })] }).handler,
    )
    renderApp('/')

    const recurring = (await screen.findByText('Alquiler')).closest('li') as HTMLElement
    const oneOff = screen.getByText('Regalo').closest('li') as HTMLElement
    expect(recurring).toHaveClass('expense--recurring')
    expect(oneOff).not.toHaveClass('expense--recurring')
  })
})
