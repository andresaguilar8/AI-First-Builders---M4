import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { server } from '../../msw/server'
import { renderApp } from '../../renderApp'

const withoutSession = http.get('/api/auth/me', () => new HttpResponse(null, { status: 401 }))

async function submitLogin(email: string, password: string) {
  const user = userEvent.setup()
  await user.type(await screen.findByLabelText('Email'), email)
  await user.type(screen.getByLabelText('Contraseña'), password)
  await user.click(screen.getByRole('button', { name: 'Ingresar' }))
}

describe('LoginPage', () => {
  it('AC-52: con credenciales correctas entra a la vista del mes', async () => {
    let loggedIn = false
    server.use(
      http.post('/api/auth/login', () => {
        loggedIn = true
        return new HttpResponse(null, { status: 204 })
      }),
      http.get('/api/auth/me', () =>
        loggedIn
          ? HttpResponse.json({ email: 'andres@test.local' })
          : new HttpResponse(null, { status: 401 }),
      ),
    )
    const router = renderApp('/login')

    await submitLogin('andres@test.local', 'Correcta-1')

    expect(await screen.findByRole('heading', { name: 'Gastos del mes' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/')
  })

  it('AC-53: con credenciales inválidas muestra un error y no muestra datos', async () => {
    server.use(
      withoutSession,
      http.post('/api/auth/login', () => new HttpResponse(null, { status: 401 })),
    )
    const router = renderApp('/login')

    await submitLogin('andres@test.local', 'incorrecta')

    expect(await screen.findByRole('alert')).toHaveTextContent('Email o contraseña incorrectos')
    expect(screen.queryByRole('heading', { name: 'Gastos del mes' })).not.toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/login')
  })

  it('AC-54: sin sesión, la vista del mes redirige al login', async () => {
    server.use(withoutSession)
    const router = renderApp('/')

    expect(await screen.findByRole('button', { name: 'Ingresar' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Gastos del mes' })).not.toBeInTheDocument()
    await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
  })

  it('AC-54: si la sesión vence mientras se usa la app, vuelve al login', async () => {
    let sessionValid = true
    server.use(
      http.get('/api/auth/me', () =>
        sessionValid
          ? HttpResponse.json({ email: 'andres@test.local' })
          : new HttpResponse(null, { status: 401 }),
      ),
    )
    const router = renderApp('/')
    expect(await screen.findByRole('heading', { name: 'Gastos del mes' })).toBeInTheDocument()

    sessionValid = false
    window.dispatchEvent(new Event('gg:session-expired'))

    await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
  })

  it('AC-57: no existe enlace ni ruta de registro', async () => {
    server.use(withoutSession)
    renderApp('/login')

    await screen.findByRole('button', { name: 'Ingresar' })
    expect(screen.queryByRole('link', { name: /regist|crear cuenta/i })).not.toBeInTheDocument()
  })

  it('AC-57: la ruta /registro no muestra ningún formulario de registro', async () => {
    server.use(withoutSession)
    const router = renderApp('/registro')

    await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
    expect(screen.queryByText(/regist/i)).not.toBeInTheDocument()
  })
})
