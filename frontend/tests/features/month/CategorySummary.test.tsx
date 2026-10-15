import { screen, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import type { MonthView } from '../../../src/api/types'
import { server } from '../../msw/server'
import { withSession } from '../../msw/month'
import { renderApp } from '../../renderApp'

const october: MonthView = {
  period: '2026-10',
  today: '2026-10-05',
  expenses: [],
  total: '450000.00',
  pendingTotal: '100000.00',
  byCategory: [
    { categoryId: crypto.randomUUID(), categoryName: 'Servicios', total: '100000.00' },
    { categoryId: crypto.randomUUID(), categoryName: 'Vivienda', total: '300000.00' },
    { categoryId: null, categoryName: 'Sin categoría', total: '50000.00' },
  ],
}

describe('Totales y resumen por categoría', () => {
  it('AC-32, AC-33: muestra el total de cada categoría y de "Sin categoría" en formato es-AR', async () => {
    server.use(
      withSession,
      http.get('/api/periods/:period', () => HttpResponse.json(october)),
    )
    renderApp('/')

    const summary = await screen.findByRole('region', { name: 'Resumen por categoría' })
    const rows = within(summary).getAllByRole('listitem')
    // formatArs separa el signo con un espacio no separable.
    expect(rows.map((row) => row.textContent?.replace(/\s+/g, ' '))).toEqual([
      'Servicios$ 100.000,00',
      'Vivienda$ 300.000,00',
      'Sin categoría$ 50.000,00',
    ])
  })

  it('AC-34: muestra el total del mes y el total pendiente', async () => {
    server.use(
      withSession,
      http.get('/api/periods/:period', () => HttpResponse.json(october)),
    )
    renderApp('/')

    const totals = await screen.findByRole('region', { name: 'Totales del mes' })
    expect(within(totals).getByText('Total del mes').nextSibling).toHaveTextContent('$ 450.000,00')
    expect(within(totals).getByText('Pendiente de pago').nextSibling).toHaveTextContent(
      '$ 100.000,00',
    )
  })

  it('un mes sin gastos no muestra resumen por categoría', async () => {
    server.use(
      withSession,
      http.get('/api/periods/:period', () =>
        HttpResponse.json({ ...october, total: '0.00', pendingTotal: '0.00', byCategory: [] }),
      ),
    )
    renderApp('/')

    await screen.findByRole('region', { name: 'Totales del mes' })
    expect(screen.queryByRole('region', { name: 'Resumen por categoría' })).not.toBeInTheDocument()
  })
})
