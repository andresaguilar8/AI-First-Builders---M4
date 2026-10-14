import { describe, expect, it } from 'vitest'
import { formatDate, formatPeriod, nextPeriod, periodOf, previousPeriod } from '../../src/lib/dates'

describe('fechas y períodos', () => {
  it('formatea una fecha yyyy-MM-dd como dd/MM/yyyy', () => {
    expect(formatDate('2026-10-05')).toBe('05/10/2026')
  })

  it('obtiene el período de una fecha', () => {
    expect(periodOf('2026-10-05')).toBe('2026-10')
  })

  it('AC-50: el mes anterior a septiembre es agosto', () => {
    expect(previousPeriod('2026-09')).toBe('2026-08')
  })

  it('AC-51: el mes anterior a enero de 2027 es diciembre de 2026', () => {
    expect(previousPeriod('2027-01')).toBe('2026-12')
  })

  it('el mes siguiente a diciembre es enero del año siguiente', () => {
    expect(nextPeriod('2026-12')).toBe('2027-01')
  })

  it('muestra el período con el nombre del mes', () => {
    expect(formatPeriod('2026-10')).toBe('octubre de 2026')
  })
})
