import type { CategoryTotal } from '../../api/types'
import { formatArs } from '../../lib/amount'

interface CategorySummaryProps {
  byCategory: CategoryTotal[]
}

/** Total del mes por categoría, sumando pagados y pendientes; incluye "Sin categoría" (RF-23, RF-24). */
export function CategorySummary({ byCategory }: CategorySummaryProps) {
  if (byCategory.length === 0) return null

  return (
    <section className="category-summary" aria-label="Resumen por categoría">
      <h3>Resumen por categoría</h3>
      <ul>
        {byCategory.map((category) => (
          <li key={category.categoryId ?? 'sin-categoria'}>
            <span>{category.categoryName}</span>
            <span>{formatArs(category.total)}</span>
          </li>
        ))}
      </ul>
    </section>
  )
}
