import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect } from 'react'
import { Navigate, Outlet, useNavigate } from 'react-router'
import { ApiError, SESSION_EXPIRED_EVENT } from '../../api/client'
import { fetchMe, meQueryKey } from './session'

/** Muestra las rutas hijas solo con sesión válida; si no, o si vence, va al login (AC-54). */
export function RequireAuth() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const me = useQuery({ queryKey: meQueryKey, queryFn: fetchMe })

  useEffect(() => {
    const onSessionExpired = () => {
      queryClient.clear()
      navigate('/login', { replace: true })
    }
    window.addEventListener(SESSION_EXPIRED_EVENT, onSessionExpired)
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, onSessionExpired)
  }, [navigate, queryClient])

  if (me.isPending) return <p>Cargando…</p>
  if (me.error instanceof ApiError && me.error.status === 401)
    return <Navigate to="/login" replace />
  if (me.isError)
    return <p role="alert">No se pudo verificar la sesión. Probá recargar la página.</p>

  return <Outlet />
}
