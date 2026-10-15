import { api } from '../../api/client'
import type { Category } from '../../api/types'

export const categoriesQueryKey = ['categories'] as const

export function fetchCategories() {
  return api.get<Category[]>('/api/categories')
}
