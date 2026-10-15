import { Navigate, type RouteObject } from 'react-router'
import { LoginPage } from './features/auth/LoginPage'
import { RequireAuth } from './features/auth/RequireAuth'
import { MonthPage } from './features/month/MonthPage'

export const routes: RouteObject[] = [
  { path: '/login', element: <LoginPage /> },
  {
    element: <RequireAuth />,
    children: [
      // La vista de categorías llega con US6.
      { path: '/', element: <MonthPage /> },
      { path: '/categorias', element: <h1>Categorías</h1> },
    ],
  },
  // Cualquier otra ruta (p. ej. /registro, que no existe: RF-37) vuelve al inicio.
  { path: '*', element: <Navigate to="/" replace /> },
]
