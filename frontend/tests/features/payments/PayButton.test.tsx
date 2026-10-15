import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import type { MonthExpense } from '../../../src/api/types'
import { server } from '../../msw/server'
import { monthExpense, monthHandler, paymentHandlers, withSession } from '../../msw/month'
import { renderApp } from '../../renderApp'

const TODAY = '2026-10-05'

async function setup(expense: MonthExpense) {
  const expenses = { '2026-10': [expense] }
  const payment = paymentHandlers(expenses, TODAY)
  server.use(withSession, monthHandler(TODAY, expenses).handler, ...payment.handlers)
  renderApp('/')
  const item = (await screen.findByText(expense.name)).closest('li') as HTMLElement
  return { user: userEvent.setup(), item, calls: payment.calls }
}

const overdue = monthExpense({ name: 'Gas', dueDate: '2026-10-01', status: 'Vencido' })

describe('PayButton', () => {
  it('AC-35: pagar un gasto no vencido no pide fecha y se ve pagado hoy sin recargar', async () => {
    const { user, item, calls } = await setup(
      monthExpense({ name: 'Luz', dueDate: '2026-10-10', status: 'AlDia' }),
    )

    await user.click(within(item).getByRole('button', { name: 'Marcar Luz como pagado' }))

    expect(await within(item).findByText('Pagado el 05/10/2026')).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(calls).toEqual([{ method: 'PUT', body: undefined }])
  })

  it('AC-37: pagar un vencido pregunta "¿Lo pagaste hoy?" y con "Sí" registra hoy', async () => {
    const { user, item, calls } = await setup(overdue)

    await user.click(within(item).getByRole('button', { name: 'Marcar Gas como pagado' }))
    const dialog = await screen.findByRole('dialog', { name: '¿Lo pagaste hoy?' })
    await user.click(within(dialog).getByRole('button', { name: 'Sí, hoy' }))

    expect(await within(item).findByText('Pagado el 05/10/2026')).toBeInTheDocument()
    expect(calls).toEqual([{ method: 'PUT', body: undefined }])
  })

  it('AC-36: pagar un vencido permite ingresar la fecha real', async () => {
    const { user, item, calls } = await setup(overdue)

    await user.click(within(item).getByRole('button', { name: 'Marcar Gas como pagado' }))
    const dialog = await screen.findByRole('dialog', { name: '¿Lo pagaste hoy?' })
    await user.click(within(dialog).getByRole('button', { name: 'No, otra fecha' }))
    await user.type(within(dialog).getByLabelText('Fecha de pago'), '2026-10-03')
    await user.click(within(dialog).getByRole('button', { name: 'Confirmar' }))

    expect(await within(item).findByText('Pagado el 03/10/2026')).toBeInTheDocument()
    expect(calls).toEqual([{ method: 'PUT', body: { paidOn: '2026-10-03' } }])
  })

  it('AC-38: rechaza una fecha de pago posterior a hoy y el gasto sigue pendiente', async () => {
    const { user, item, calls } = await setup(overdue)

    await user.click(within(item).getByRole('button', { name: 'Marcar Gas como pagado' }))
    const dialog = await screen.findByRole('dialog', { name: '¿Lo pagaste hoy?' })
    await user.click(within(dialog).getByRole('button', { name: 'No, otra fecha' }))
    expect(within(dialog).getByLabelText('Fecha de pago')).toHaveAttribute('max', TODAY)
    await user.type(within(dialog).getByLabelText('Fecha de pago'), '2026-10-06')
    await user.click(within(dialog).getByRole('button', { name: 'Confirmar' }))

    expect(
      await within(dialog).findByText('La fecha de pago no puede ser posterior a hoy.'),
    ).toBeInTheDocument()
    expect(calls).toHaveLength(0)
    expect(within(item).getByText('Vencido')).toBeInTheDocument()
  })

  it('AC-39: desmarcar un pagado lo deja pendiente y sin fecha de pago', async () => {
    const { user, item, calls } = await setup(
      monthExpense({ name: 'Luz', dueDate: '2026-10-10', status: 'AlDia' }),
    )
    await user.click(within(item).getByRole('button', { name: 'Marcar Luz como pagado' }))
    await within(item).findByText('Pagado el 05/10/2026')

    await user.click(within(item).getByRole('button', { name: 'Desmarcar pago de Luz' }))

    expect(await within(item).findByText('Al día')).toBeInTheDocument()
    expect(within(item).queryByText(/Pagado el/)).not.toBeInTheDocument()
    expect(calls.map((c) => c.method)).toEqual(['PUT', 'DELETE'])
  })

  it('muestra el estado de vencimiento de cada gasto pendiente', async () => {
    const statuses: [MonthExpense['status'], string][] = [
      ['AlDia', 'Al día'],
      ['ProximoAVencer', 'Próximo a vencer'],
      ['VenceHoy', 'Vence hoy'],
      ['Vencido', 'Vencido'],
    ]
    server.use(
      withSession,
      monthHandler(TODAY, {
        '2026-10': statuses.map(([status], i) => monthExpense({ name: `Gasto ${i}`, status })),
      }).handler,
    )
    renderApp('/')

    for (const [i, [, label]] of statuses.entries()) {
      const item = (await screen.findByText(`Gasto ${i}`)).closest('li') as HTMLElement
      expect(within(item).getByText(label)).toBeInTheDocument()
    }
  })
})
