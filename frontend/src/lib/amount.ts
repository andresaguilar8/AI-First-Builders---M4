// Montos en pesos (RF-03): la UI usa el formato es-AR y la API, un string con punto decimal.

const ARS_INPUT = /^(\d{1,3}(?:\.\d{3})*|\d+)(?:,(\d{1,2}))?$/
const MAX_INTEGER_DIGITS = 12

const arsFormatter = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' })

/**
 * Convierte un monto ingresado con coma decimal y punto de miles opcional (`1.234,56`) al formato
 * de la API (`1234.56`). Devuelve `null` si no es mayor a 0, supera $999.999.999.999,99 o tiene
 * más de 2 decimales.
 */
export function parseArsAmount(input: string): string | null {
  const match = ARS_INPUT.exec(input.trim())
  if (!match) return null

  const integer = match[1].replaceAll('.', '').replace(/^0+(?=\d)/, '')
  const decimals = (match[2] ?? '').padEnd(2, '0')

  if (integer.length > MAX_INTEGER_DIGITS) return null
  if (/^0+$/.test(integer + decimals)) return null

  return `${integer}.${decimals}`
}

/** Formatea un monto de la API (`1234.56`) como `$ 1.234,56`. */
export function formatArs(amount: string): string {
  return arsFormatter.format(Number(amount))
}
