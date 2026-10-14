import { createBrowserRouter, RouterProvider } from 'react-router'

// Las páginas reales llegan con cada historia: login (US1), mes (US2) y categorías (US6).
const router = createBrowserRouter([
  { path: '/login', element: <h1>Iniciar sesión</h1> },
  { path: '/', element: <h1>Gastos del mes</h1> },
  { path: '/categorias', element: <h1>Categorías</h1> },
])

function App() {
  return <RouterProvider router={router} />
}

export default App
