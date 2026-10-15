import { Navigate, type RouteObject } from 'react-router'
import { LoginPage } from './features/auth/LoginPage'
import { RequireAuth } from './features/auth/RequireAuth'

export const routes: RouteObject[] = [
  { path: '/login', element: <LoginPage /> },
  {
    element: <RequireAuth />,
    children: [
      // La vista del mes llega con US2 y la de categorías con US6.
      { path: '/', element: <h1>Gastos del mes</h1> },
      { path: '/categorias', element: <h1>Categorías</h1> },
    ],
  },
  // Cualquier otra ruta (p. ej. /registro, que no existe: RF-37) vuelve al inicio.
  { path: '*', element: <Navigate to="/" replace /> },
]
