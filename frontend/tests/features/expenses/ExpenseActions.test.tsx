import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import type { MonthExpense } from '../../../src/api/types'
import { server } from '../../msw/server'
import { monthExpense, monthHandler, withSession } from '../../msw/month'
import { renderApp } from '../../renderApp'

interface Call {
  method: string
  path: string
  body?: unknown
}

/** Responde las acciones sobre gastos quitando el gasto del mes y registra las llamadas. */
function actionHandlers(expenses: Record<string, MonthExpense[]>) {
  const calls: Call[] = []
  const remove = (period: string, id: string) => {
    expenses[period] = expenses[period].filter((e) => e.id !== id)
  }
  const base = '/api/periods/:period/expenses/:id'
  const handlers = [
    http.delete(base, ({ params, request }) => {
      calls.push({ method: 'DELETE', path: new URL(request.url).pathname })
      remove(String(params.period), String(params.id))
      return new HttpResponse(null, { status: 204 })
    }),
    http.post(`${base}/discontinue`, ({ params, request }) => {
      calls.push({ method: 'POST', path: new URL(request.url).pathname })
      remove(String(params.period), String(params.id))
      return new HttpResponse(null, { status: 204 })
    }),
    http.post(`${base}/convert`, async ({ params, request }) => {
      const body = (await request.json()) as { to: MonthExpense['kind'] }
      calls.push({ method: 'POST', path: new URL(request.url).pathname, body })
      const period = String(params.period)
      const current = expenses[period].find((e) => e.id === params.id) as MonthExpense
      const converted = { ...current, id: crypto.randomUUID(), kind: body.to }
      expenses[period] = expenses[period].map((e) => (e.id === current.id ? converted : e))
      return HttpResponse.json(converted)
    }),
  ]
  return { handlers, calls }
}

async function setup(expense: MonthExpense) {
  const expenses = { '2026-10': [expense] }
  const actions = actionHandlers(expenses)
  server.use(withSession, monthHandler('2026-10-05', expenses).handler, ...actions.handlers)
  renderApp('/')
  const item = (await screen.findByText(expense.name)).closest('li') as HTMLElement
  return { user: userEvent.setup(), item, calls: actions.calls }
}

const gift = monthExpense({ name: 'Regalo' })
const rent = monthExpense({ name: 'Alquiler', kind: 'Recurring' })

describe('ExpenseActions', () => {
  it('un puntual ofrece "Eliminar" y "Hacer recurrente"', async () => {
    const { item } = await setup(gift)

    expect(within(item).getByRole('button', { name: 'Eliminar Regalo' })).toBeInTheDocument()
    expect(
      within(item).getByRole('button', { name: 'Hacer recurrente Regalo' }),
    ).toBeInTheDocument()
    expect(within(item).queryByRole('button', { name: /Dar de baja/ })).not.toBeInTheDocument()
    expect(within(item).queryByRole('button', { name: /Hacer puntual/ })).not.toBeInTheDocument()
  })

  it('un recurrente ofrece "Dar de baja desde este mes" y "Hacer puntual"', async () => {
    const { item } = await setup(rent)

    expect(
      within(item).getByRole('button', { name: 'Dar de baja desde este mes Alquiler' }),
    ).toBeInTheDocument()
    expect(within(item).getByRole('button', { name: 'Hacer puntual Alquiler' })).toBeInTheDocument()
    expect(within(item).queryByRole('button', { name: /Eliminar/ })).not.toBeInTheDocument()
  })

  it('eliminar un puntual pide confirmación y lo quita del mes', async () => {
    const { user, item, calls } = await setup(gift)

    await user.click(within(item).getByRole('button', { name: 'Eliminar Regalo' }))
    const dialog = await screen.findByRole('dialog', { name: '¿Eliminar «Regalo»?' })
    expect(calls).toHaveLength(0)
    await user.click(within(dialog).getByRole('button', { name: 'Confirmar' }))

    await waitFor(() => expect(screen.queryByText('Regalo')).not.toBeInTheDocument())
    expect(calls).toEqual([{ method: 'DELETE', path: `/api/periods/2026-10/expenses/${gift.id}` }])
  })

  it('cancelar la confirmación no hace nada', async () => {
    const { user, item, calls } = await setup(gift)

    await user.click(within(item).getByRole('button', { name: 'Eliminar Regalo' }))
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', { name: 'Cancelar' }),
    )

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByText('Regalo')).toBeInTheDocument()
    expect(calls).toHaveLength(0)
  })

  it('dar de baja un recurrente pide confirmación y lo quita del mes', async () => {
    const { user, item, calls } = await setup(rent)

    await user.click(
      within(item).getByRole('button', { name: 'Dar de baja desde este mes Alquiler' }),
    )
    const dialog = await screen.findByRole('dialog', {
      name: '¿Dar de baja «Alquiler» desde octubre de 2026?',
    })
    await user.click(within(dialog).getByRole('button', { name: 'Confirmar' }))

    await waitFor(() => expect(screen.queryByText('Alquiler')).not.toBeInTheDocument())
    expect(calls).toEqual([
      { method: 'POST', path: `/api/periods/2026-10/expenses/${rent.id}/discontinue` },
    ])
  })

  it('hacer recurrente un puntual lo convierte tras confirmar', async () => {
    const { user, item, calls } = await setup(gift)

    await user.click(within(item).getByRole('button', { name: 'Hacer recurrente Regalo' }))
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', { name: 'Confirmar' }),
    )

    expect(await screen.findByText('Recurrente')).toBeInTheDocument()
    expect(calls[0]).toMatchObject({ body: { to: 'Recurring' } })
  })

  it('hacer puntual un recurrente lo convierte tras confirmar', async () => {
    const { user, item, calls } = await setup(rent)

    await user.click(within(item).getByRole('button', { name: 'Hacer puntual Alquiler' }))
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', { name: 'Confirmar' }),
    )

    expect(await screen.findByText('Puntual')).toBeInTheDocument()
    expect(calls[0]).toMatchObject({ body: { to: 'OneOff' } })
  })
})
