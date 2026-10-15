import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { api, ApiError, ValidationError } from '../../api/client'
import type { Category, CategoryInput } from '../../api/types'
import { categoriesQueryKey, fetchCategories } from './categoryQuery'

const NAME_MAX_LENGTH = 50

/** Mensaje de error de un alta o renombrado rechazado (RF-18, RF-19). */
function errorMessage(error: Error | null): string | null {
  if (!error) return null
  if (error instanceof ApiError && error.status === 409)
    return 'Ya existe una categoría con ese nombre o el nombre está reservado.'
  if (error instanceof ValidationError) return Object.values(error.errors).flat().join(' ')
  return 'No se pudo guardar la categoría. Probá de nuevo.'
}

/** Alta, renombrado y eliminación de categorías (RF-18 a RF-21). */
export function CategoriesPage() {
  const queryClient = useQueryClient()
  const categories = useQuery({ queryKey: categoriesQueryKey, queryFn: fetchCategories })
  const [name, setName] = useState('')
  const [nameError, setNameError] = useState<string | null>(null)

  // Renombrar o eliminar cambia lo que muestran todos los meses (RF-20, RF-21).
  const refresh = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: categoriesQueryKey }),
      queryClient.invalidateQueries({ queryKey: ['month'] }),
    ])

  const create = useMutation({
    mutationFn: (input: CategoryInput) => api.post<Category>('/api/categories', input),
    onSuccess: async () => {
      setName('')
      await refresh()
    },
  })

  const handleCreate = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!name.trim()) return setNameError('El nombre es obligatorio.')
    setNameError(null)
    create.mutate({ name: name.trim() })
  }

  return (
    <main className="categories">
      <nav className="page-nav">
        <Link to="/">Volver al mes</Link>
      </nav>
      <h1>Categorías</h1>

      <form className="category-form" onSubmit={handleCreate} noValidate>
        <label htmlFor="category-name">Nueva categoría</label>
        <div className="inline-field">
          <input
            id="category-name"
            value={name}
            maxLength={NAME_MAX_LENGTH}
            onChange={(event) => setName(event.target.value)}
          />
          <button type="submit" disabled={create.isPending}>
            Crear
          </button>
        </div>
        {(nameError ?? errorMessage(create.error)) && (
          <p className="form-error">{nameError ?? errorMessage(create.error)}</p>
        )}
      </form>

      {categories.isError && (
        <p role="alert" className="form-error">
          No se pudieron cargar las categorías. Probá recargar la página.
        </p>
      )}
      {categories.data?.length === 0 && <p className="empty">Todavía no hay categorías.</p>}
      <ul className="category-list">
        {categories.data?.map((category) => (
          <CategoryItem key={category.id} category={category} onChanged={refresh} />
        ))}
      </ul>
    </main>
  )
}

interface CategoryItemProps {
  category: Category
  onChanged: () => Promise<unknown>
}

function CategoryItem({ category, onChanged }: CategoryItemProps) {
  const [renaming, setRenaming] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [newName, setNewName] = useState(category.name)
  const [nameError, setNameError] = useState<string | null>(null)
  const path = `/api/categories/${category.id}`

  const rename = useMutation({
    mutationFn: (input: CategoryInput) => api.put<Category>(path, input),
    onSuccess: async () => {
      setRenaming(false)
      await onChanged()
    },
  })

  const remove = useMutation({
    mutationFn: () => api.delete<undefined>(path),
    onSettled: onChanged,
  })

  const handleRename = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!newName.trim()) return setNameError('El nombre es obligatorio.')
    setNameError(null)
    rename.mutate({ name: newName.trim() })
  }

  const error = nameError ?? errorMessage(rename.error)

  return (
    <li className="category">
      {renaming ? (
        <form onSubmit={handleRename} noValidate>
          <label htmlFor={`rename-${category.id}`}>Nuevo nombre de {category.name}</label>
          <div className="inline-field">
            <input
              id={`rename-${category.id}`}
              value={newName}
              maxLength={NAME_MAX_LENGTH}
              onChange={(event) => setNewName(event.target.value)}
            />
            <button type="submit" disabled={rename.isPending}>
              Guardar
            </button>
            <button
              type="button"
              onClick={() => {
                setRenaming(false)
                setNewName(category.name)
              }}
            >
              Cancelar
            </button>
          </div>
          {error && <p className="form-error">{error}</p>}
        </form>
      ) : (
        <>
          <span>{category.name}</span>
          <div className="expense-actions">
            <button
              type="button"
              onClick={() => setRenaming(true)}
              aria-label={`Renombrar ${category.name}`}
            >
              Renombrar
            </button>
            <button
              type="button"
              disabled={remove.isPending}
              onClick={() => setConfirmingDelete(true)}
              aria-label={`Eliminar ${category.name}`}
            >
              Eliminar
            </button>
          </div>
        </>
      )}
      {confirmingDelete && (
        <div
          className="dialog"
          role="dialog"
          aria-modal="true"
          aria-labelledby={`delete-${category.id}`}
        >
          <h3 id={`delete-${category.id}`}>¿Eliminar la categoría «{category.name}»?</h3>
          <p>Sus gastos pasan a «Sin categoría» en todos los meses.</p>
          <div className="dialog-actions">
            <button
              type="button"
              onClick={() => {
                setConfirmingDelete(false)
                remove.mutate()
              }}
            >
              Confirmar
            </button>
            <button type="button" onClick={() => setConfirmingDelete(false)}>
              Cancelar
            </button>
          </div>
        </div>
      )}
      {remove.isError && (
        <p role="alert" className="form-error">
          No se pudo eliminar la categoría. Probá de nuevo.
        </p>
      )}
    </li>
  )
}
