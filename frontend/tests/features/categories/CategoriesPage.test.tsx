import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { server } from '../../msw/server'
import { categoryHandlers, monthHandler, withSession } from '../../msw/month'
import { renderApp } from '../../renderApp'

const education = { id: crypto.randomUUID(), name: 'Educación' }
const health = { id: crypto.randomUUID(), name: 'Salud' }

function setup() {
  const categories = categoryHandlers([education, health])
  server.use(withSession, ...categories.handlers)
  renderApp('/categorias')
  return { user: userEvent.setup(), categories }
}

describe('CategoriesPage', () => {
  it('AC-25: lista las categorías y permite crear una nueva', async () => {
    const { user, categories } = setup()
    expect(await screen.findByText('Educación')).toBeInTheDocument()

    await user.type(screen.getByLabelText('Nueva categoría'), 'Vivienda')
    await user.click(screen.getByRole('button', { name: 'Crear' }))

    expect(await screen.findByText('Vivienda')).toBeInTheDocument()
    expect(screen.getByLabelText('Nueva categoría')).toHaveValue('')
    expect(categories.current().map((c) => c.name)).toEqual(['Educación', 'Salud', 'Vivienda'])
  })

  it.each(['educacion', 'Sin categoría'])(
    'AC-26, AC-27: crear "%s" muestra el rechazo por nombre duplicado o reservado',
    async (name) => {
      const { user, categories } = setup()
      await screen.findByText('Educación')

      await user.type(screen.getByLabelText('Nueva categoría'), name)
      await user.click(screen.getByRole('button', { name: 'Crear' }))

      expect(
        await screen.findByText(
          'Ya existe una categoría con ese nombre o el nombre está reservado.',
        ),
      ).toBeInTheDocument()
      expect(categories.current()).toHaveLength(2)
    },
  )

  it('no permite crear una categoría sin nombre', async () => {
    const { user, categories } = setup()
    await screen.findByText('Educación')

    await user.click(screen.getByRole('button', { name: 'Crear' }))

    expect(await screen.findByText('El nombre es obligatorio.')).toBeInTheDocument()
    expect(categories.current()).toHaveLength(2)
  })

  it('AC-28: renombra una categoría', async () => {
    const { user } = setup()
    await user.click(await screen.findByRole('button', { name: 'Renombrar Salud' }))

    const input = screen.getByLabelText('Nuevo nombre de Salud')
    await user.clear(input)
    await user.type(input, 'Salud y deporte')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByText('Salud y deporte')).toBeInTheDocument()
  })

  it('AC-26: renombrar con un nombre existente muestra el rechazo', async () => {
    const { user } = setup()
    await user.click(await screen.findByRole('button', { name: 'Renombrar Salud' }))

    const input = screen.getByLabelText('Nuevo nombre de Salud')
    await user.clear(input)
    await user.type(input, 'EDUCACIÓN')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(
      await screen.findByText('Ya existe una categoría con ese nombre o el nombre está reservado.'),
    ).toBeInTheDocument()
  })

  it('AC-29, AC-30: elimina una categoría tras confirmar', async () => {
    const { user, categories } = setup()
    await user.click(await screen.findByRole('button', { name: 'Eliminar Salud' }))

    const dialog = await screen.findByRole('dialog', { name: '¿Eliminar la categoría «Salud»?' })
    expect(dialog).toHaveTextContent('Sus gastos pasan a «Sin categoría» en todos los meses.')
    await user.click(within(dialog).getByRole('button', { name: 'Confirmar' }))

    await waitFor(() => expect(screen.queryByText('Salud')).not.toBeInTheDocument())
    expect(categories.current()).toEqual([education])
  })

  it('se navega entre la vista del mes y las categorías', async () => {
    server.use(withSession, monthHandler('2026-10-05').handler)
    const router = renderApp('/')
    const user = userEvent.setup()

    await user.click(await screen.findByRole('link', { name: 'Categorías' }))
    expect(await screen.findByRole('heading', { name: 'Categorías' })).toBeInTheDocument()

    await user.click(screen.getByRole('link', { name: 'Volver al mes' }))
    expect(await screen.findByRole('heading', { name: 'Octubre de 2026' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/')
  })
})
