import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { server } from '../../msw/server'
import { monthHandler, withSession } from '../../msw/month'
import { renderApp } from '../../renderApp'

describe('ExportButton', () => {
  it('RF-38: la vista del mes ofrece descargar el historial en CSV', async () => {
    server.use(withSession, monthHandler('2026-10-05').handler)
    renderApp('/')

    const link = await screen.findByRole('link', { name: 'Exportar historial (CSV)' })
    expect(link).toHaveAttribute('href', '/api/export.csv')
    expect(link).toHaveAttribute('download')
  })
})
