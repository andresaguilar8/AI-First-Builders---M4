import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { server } from '../../msw/server'
import { alertHandler, monthHandler, withSession } from '../../msw/month'
import { renderApp } from '../../renderApp'

describe('DueTodayAlert', () => {
  it('AC-41: con sesión vigente, al abrir la app se ve la alerta de lo que vence hoy', async () => {
    server.use(withSession, monthHandler('2026-10-05').handler, alertHandler(1))
    renderApp('/')

    expect(await screen.findByRole('alert')).toHaveTextContent('Tenés 1 gasto que vence hoy.')
  })

  it('AC-40: al iniciar sesión se ve la alerta de lo que vence hoy', async () => {
    let loggedIn = false
    server.use(
      http.get('/api/auth/me', () =>
        loggedIn
          ? HttpResponse.json({ email: 'andres@test.local' })
          : new HttpResponse(null, { status: 401 }),
      ),
      http.post('/api/auth/login', () => {
        loggedIn = true
        return new HttpResponse(null, { status: 204 })
      }),
      monthHandler('2026-10-05').handler,
      alertHandler(2),
    )
    renderApp('/login')
    const user = userEvent.setup()

    await user.type(await screen.findByLabelText('Email'), 'andres@test.local')
    await user.type(screen.getByLabelText('Contraseña'), 'Correcta-1')
    await user.click(screen.getByRole('button', { name: 'Ingresar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Tenés 2 gastos que vencen hoy.')
  })

  it('AC-42: sin pendientes que venzan hoy no se muestra ninguna alerta', async () => {
    server.use(withSession, monthHandler('2026-10-05').handler, alertHandler(0))
    renderApp('/')

    await screen.findByRole('heading', { name: 'Octubre de 2026' })
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})
