import { Navigate, type RouteObject } from 'react-router'
import { LoginPage } from './features/auth/LoginPage'
import { RequireAuth } from './features/auth/RequireAuth'
import { CategoriesPage } from './features/categories/CategoriesPage'
import { MonthPage } from './features/month/MonthPage'

export const routes: RouteObject[] = [
  { path: '/login', element: <LoginPage /> },
  {
    element: <RequireAuth />,
    children: [
      { path: '/', element: <MonthPage /> },
      { path: '/categorias', element: <CategoriesPage /> },
    ],
  },
  // Cualquier otra ruta (p. ej. /registro, que no existe: RF-37) vuelve al inicio.
  { path: '*', element: <Navigate to="/" replace /> },
]
