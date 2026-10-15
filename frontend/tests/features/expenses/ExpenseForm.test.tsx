import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import type { CreateExpenseRequest, MonthExpense } from '../../../src/api/types'
import { server } from '../../msw/server'
import { categoryHandlers, monthExpense, monthHandler, withSession } from '../../msw/month'
import { renderApp } from '../../renderApp'

/** Registra los POST de alta y responde 201 agregando el gasto al mes. */
function createHandler(expenses: Record<string, MonthExpense[]>) {
  const posted: CreateExpenseRequest[] = []
  const handler = http.post('/api/periods/:period/expenses', async ({ params, request }) => {
    const body = (await request.json()) as CreateExpenseRequest
    posted.push(body)
    const created = monthExpense({ ...body, amount: body.amount })
    const period = String(params.period)
    expenses[period] = [...(expenses[period] ?? []), created]
    return HttpResponse.json(created, { status: 201 })
  })
  return { handler, posted }
}

async function openForm() {
  const user = userEvent.setup()
  await user.click(await screen.findByRole('button', { name: 'Agregar gasto' }))
  return user
}

describe('ExpenseForm', () => {
  it('AC-03: rechaza el alta sin nombre ni monto y no llama a la API', async () => {
    const create = createHandler({})
    server.use(withSession, monthHandler('2026-10-05').handler, create.handler)
    renderApp('/')
    const user = await openForm()

    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByText('El nombre es obligatorio.')).toBeInTheDocument()
    expect(screen.getByText('El monto es obligatorio.')).toBeInTheDocument()
    expect(create.posted).toHaveLength(0)
  })

  it('AC-05: rechaza el monto 10,999 y no llama a la API', async () => {
    const create = createHandler({})
    server.use(withSession, monthHandler('2026-10-05').handler, create.handler)
    renderApp('/')
    const user = await openForm()

    await user.type(screen.getByLabelText('Nombre'), 'Luz')
    await user.type(screen.getByLabelText('Monto'), '10,999')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(
      await screen.findByText('Ingresá un monto mayor a 0, con hasta 2 decimales (ej.: 1.234,56).'),
    ).toBeInTheDocument()
    expect(create.posted).toHaveLength(0)
  })

  it('AC-08: rechaza un vencimiento anterior al mes visualizado', async () => {
    const create = createHandler({})
    server.use(withSession, monthHandler('2026-10-05').handler, create.handler)
    renderApp('/')
    const user = await openForm()

    await user.type(screen.getByLabelText('Nombre'), 'Luz')
    await user.type(screen.getByLabelText('Monto'), '100')
    await user.type(screen.getByLabelText('Vencimiento (opcional)'), '2026-09-30')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(
      await screen.findByText('El vencimiento no puede ser anterior al mes del gasto.'),
    ).toBeInTheDocument()
    expect(create.posted).toHaveLength(0)
  })

  it('AC-06: envía el monto 1.234,56 como 1234.56 y muestra el gasto creado', async () => {
    const expenses: Record<string, MonthExpense[]> = {}
    const create = createHandler(expenses)
    server.use(withSession, monthHandler('2026-10-05', expenses).handler, create.handler)
    renderApp('/')
    const user = await openForm()

    await user.type(screen.getByLabelText('Nombre'), 'Luz')
    await user.type(screen.getByLabelText('Monto'), '1.234,56')
    await user.type(screen.getByLabelText('Descripción (opcional)'), 'Edesur')
    await user.type(screen.getByLabelText('Vencimiento (opcional)'), '2026-11-05')
    await user.click(screen.getByLabelText('Recurrente'))
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByText('$ 1.234,56')).toBeInTheDocument()
    expect(create.posted).toEqual([
      {
        name: 'Luz',
        amount: '1234.56',
        description: 'Edesur',
        categoryId: null,
        dueDate: '2026-11-05',
        kind: 'Recurring',
      },
    ])
    expect(screen.queryByRole('button', { name: 'Guardar' })).not.toBeInTheDocument()
  })

  it('muestra los errores por campo que devuelve la API', async () => {
    server.use(
      withSession,
      monthHandler('2026-10-05').handler,
      http.post('/api/periods/:period/expenses', () =>
        HttpResponse.json(
          { title: 'Datos inválidos', status: 400, errors: { name: ['Nombre rechazado.'] } },
          { status: 400 },
        ),
      ),
    )
    renderApp('/')
    const user = await openForm()

    await user.type(screen.getByLabelText('Nombre'), 'Luz')
    await user.type(screen.getByLabelText('Monto'), '100')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByText('Nombre rechazado.')).toBeInTheDocument()
  })

  it('AC-31: permite asociar una categoría existente al gasto', async () => {
    const services = { id: crypto.randomUUID(), name: 'Servicios' }
    const expenses: Record<string, MonthExpense[]> = {}
    const create = createHandler(expenses)
    server.use(
      withSession,
      monthHandler('2026-10-05', expenses).handler,
      create.handler,
      ...categoryHandlers([services]).handlers,
    )
    renderApp('/')
    const user = await openForm()

    await user.type(screen.getByLabelText('Nombre'), 'Luz')
    await user.type(screen.getByLabelText('Monto'), '100')
    const select = screen.getByLabelText('Categoría (opcional)')
    expect(within(select).getByRole('option', { name: 'Sin categoría' })).toBeInTheDocument()
    await user.selectOptions(
      select,
      await within(select).findByRole('option', { name: 'Servicios' }),
    )
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    await screen.findByText('Luz')
    expect(create.posted[0].categoryId).toBe(services.id)
  })
})
