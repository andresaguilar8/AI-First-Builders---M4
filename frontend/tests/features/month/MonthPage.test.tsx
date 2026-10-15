import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { server } from '../../msw/server'
import { monthExpense, monthHandler, withSession } from '../../msw/month'
import { renderApp } from '../../renderApp'

describe('MonthPage', () => {
  it('AC-49: la carga inicial muestra el mes de la fecha actual del backend', async () => {
    const month = monthHandler('2026-10-05', {
      '2026-10': [monthExpense({ name: 'Alquiler', kind: 'Recurring', amount: '300000.00' })],
    })
    server.use(withSession, month.handler)
    renderApp('/')

    expect(await screen.findByRole('heading', { name: 'Octubre de 2026' })).toBeInTheDocument()
    expect(await screen.findByText('Alquiler')).toBeInTheDocument()
    expect(month.requested.at(-1)).toBe('2026-10')
  })

  it('AC-50: desde septiembre, el mes anterior es agosto del mismo año', async () => {
    const month = monthHandler('2026-09-10', {
      '2026-08': [monthExpense({ name: 'Seguro de agosto' })],
    })
    server.use(withSession, month.handler)
    renderApp('/')
    await screen.findByRole('heading', { name: 'Septiembre de 2026' })

    await userEvent.setup().click(screen.getByRole('button', { name: 'Mes anterior' }))

    expect(await screen.findByRole('heading', { name: 'Agosto de 2026' })).toBeInTheDocument()
    expect(await screen.findByText('Seguro de agosto')).toBeInTheDocument()
    expect(month.requested.at(-1)).toBe('2026-08')
  })

  it('AC-51: desde enero de 2027, el mes anterior es diciembre de 2026', async () => {
    const month = monthHandler('2027-01-10')
    server.use(withSession, month.handler)
    renderApp('/')
    await screen.findByRole('heading', { name: 'Enero de 2027' })

    await userEvent.setup().click(screen.getByRole('button', { name: 'Mes anterior' }))

    expect(await screen.findByRole('heading', { name: 'Diciembre de 2026' })).toBeInTheDocument()
    expect(month.requested.at(-1)).toBe('2026-12')
  })

  it('desde diciembre, el mes siguiente es enero del año siguiente', async () => {
    const month = monthHandler('2026-12-01')
    server.use(withSession, month.handler)
    renderApp('/')
    await screen.findByRole('heading', { name: 'Diciembre de 2026' })

    await userEvent.setup().click(screen.getByRole('button', { name: 'Mes siguiente' }))

    expect(await screen.findByRole('heading', { name: 'Enero de 2027' })).toBeInTheDocument()
    expect(month.requested.at(-1)).toBe('2027-01')
  })

  it('muestra de cada gasto su monto, categoría, vencimiento y tipo', async () => {
    server.use(
      withSession,
      monthHandler('2026-10-05', {
        '2026-10': [
          monthExpense({
            name: 'Luz',
            amount: '1234.56',
            category: { id: crypto.randomUUID(), name: 'Servicios' },
            dueDate: '2026-10-10',
            kind: 'Recurring',
          }),
          monthExpense({ name: 'Regalo' }),
        ],
      }).handler,
    )
    renderApp('/')

    const luz = (await screen.findByText('Luz')).closest('li') as HTMLElement
    expect(within(luz).getByText('$ 1.234,56')).toBeInTheDocument()
    expect(within(luz).getByText('Servicios')).toBeInTheDocument()
    expect(within(luz).getByText('Vence 10/10/2026')).toBeInTheDocument()
    expect(within(luz).getByText('Recurrente')).toBeInTheDocument()

    const regalo = screen.getByText('Regalo').closest('li') as HTMLElement
    expect(within(regalo).getByText('Sin categoría')).toBeInTheDocument()
    expect(within(regalo).getByText('Sin vencimiento')).toBeInTheDocument()
    expect(within(regalo).getByText('Puntual')).toBeInTheDocument()
  })

  it('un mes sin gastos lo indica', async () => {
    server.use(withSession, monthHandler('2026-10-05').handler)
    renderApp('/')

    expect(await screen.findByText('No hay gastos en este mes.')).toBeInTheDocument()
  })
})
