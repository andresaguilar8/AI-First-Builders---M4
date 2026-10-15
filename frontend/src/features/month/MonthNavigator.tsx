import { formatPeriod, nextPeriod, previousPeriod } from '../../lib/dates'

interface MonthNavigatorProps {
  period: string
  onChange: (period: string) => void
}

/** Mes y año visualizados, con pasaje al mes anterior y al siguiente (RF-35). */
export function MonthNavigator({ period, onChange }: MonthNavigatorProps) {
  const label = formatPeriod(period)

  return (
    <nav className="month-navigator" aria-label="Navegación entre meses">
      <button
        type="button"
        onClick={() => onChange(previousPeriod(period))}
        aria-label="Mes anterior"
      >
        ‹
      </button>
      <h2>{label.charAt(0).toUpperCase() + label.slice(1)}</h2>
      <button type="button" onClick={() => onChange(nextPeriod(period))} aria-label="Mes siguiente">
        ›
      </button>
    </nav>
  )
}
