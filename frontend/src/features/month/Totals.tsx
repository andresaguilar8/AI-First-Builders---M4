import { formatArs } from '../../lib/amount'

interface TotalsProps {
  total: string
  pendingTotal: string
}

/** Total del mes y total pendiente de pago (RF-25, RF-26). */
export function Totals({ total, pendingTotal }: TotalsProps) {
  return (
    <section className="totals" aria-label="Totales del mes">
      <dl>
        <dt>Total del mes</dt>
        <dd>{formatArs(total)}</dd>
        <dt>Pendiente de pago</dt>
        <dd>{formatArs(pendingTotal)}</dd>
      </dl>
    </section>
  )
}
