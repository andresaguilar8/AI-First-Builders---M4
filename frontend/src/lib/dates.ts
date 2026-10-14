// Fechas `yyyy-MM-dd` y períodos `yyyy-MM`, como viajan en la API. Se manipulan como texto para no
// depender de la zona horaria del navegador: "hoy" siempre lo informa el backend (RNF-07).

const monthNames = [
  'enero',
  'febrero',
  'marzo',
  'abril',
  'mayo',
  'junio',
  'julio',
  'agosto',
  'septiembre',
  'octubre',
  'noviembre',
  'diciembre',
]

function splitPeriod(period: string): [year: number, month: number] {
  const [year, month] = period.split('-').map(Number)
  return [year, month]
}

function toPeriod(year: number, month: number): string {
  return `${String(year).padStart(4, '0')}-${String(month).padStart(2, '0')}`
}

/** `2026-10-05` → `05/10/2026`. */
export function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-')
  return `${day}/${month}/${year}`
}

/** `2026-10-05` → `2026-10`. */
export function periodOf(isoDate: string): string {
  return isoDate.slice(0, 7)
}

export function nextPeriod(period: string): string {
  const [year, month] = splitPeriod(period)
  return month === 12 ? toPeriod(year + 1, 1) : toPeriod(year, month + 1)
}

export function previousPeriod(period: string): string {
  const [year, month] = splitPeriod(period)
  return month === 1 ? toPeriod(year - 1, 12) : toPeriod(year, month - 1)
}

/** `2026-10` → `octubre de 2026`. */
export function formatPeriod(period: string): string {
  const [year, month] = splitPeriod(period)
  return `${monthNames[month - 1]} de ${year}`
}
