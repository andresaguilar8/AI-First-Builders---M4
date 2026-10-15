import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { api, ApiError } from '../../api/client'
import type { LoginRequest } from '../../api/types'
import { meQueryKey } from './session'

/** Inicio de sesión con email y contraseña (RF-36). No hay registro (RF-37). */
export function LoginPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  const login = useMutation({
    mutationFn: (credentials: LoginRequest) => api.post<unknown>('/api/auth/login', credentials),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: meQueryKey })
      // AC-52: tras iniciar sesión se ve el mes actual.
      navigate('/', { replace: true })
    },
  })

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    login.mutate({ email, password })
  }

  const invalidCredentials = login.error instanceof ApiError && login.error.status === 401

  return (
    <main className="login">
      <h1>Gestor de gastos</h1>
      <form onSubmit={handleSubmit} noValidate>
        <label htmlFor="login-email">Email</label>
        <input
          id="login-email"
          type="email"
          autoComplete="username"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
        />

        <label htmlFor="login-password">Contraseña</label>
        <input
          id="login-password"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          required
        />

        {login.isError && (
          <p role="alert" className="form-error">
            {invalidCredentials
              ? 'Email o contraseña incorrectos.'
              : 'No se pudo iniciar sesión. Probá de nuevo.'}
          </p>
        )}

        <button type="submit" disabled={login.isPending}>
          Ingresar
        </button>
      </form>
    </main>
  )
}
