import { describe, expect, it } from 'vitest'
import { formatArs, parseArsAmount } from '../../src/lib/amount'

describe('parseArsAmount', () => {
  it('AC-06: convierte 1.234,56 al formato de la API', () => {
    expect(parseArsAmount('1.234,56')).toBe('1234.56')
  })

  it.each([
    ['1234,56', '1234.56'],
    ['1234', '1234.00'],
    ['1.234', '1234.00'],
    ['0,5', '0.50'],
    ['  15.000  ', '15000.00'],
  ])('acepta %s con coma decimal y punto de miles opcional', (input, expected) => {
    expect(parseArsAmount(input)).toBe(expected)
  })

  it.each(['0', '0,00', '-100'])('AC-05: rechaza %s (no es mayor a 0)', (input) => {
    expect(parseArsAmount(input)).toBeNull()
  })

  it('AC-05: rechaza 10,999 (más de 2 decimales)', () => {
    expect(parseArsAmount('10,999')).toBeNull()
  })

  it.each(['', 'abc', '1.5', '12.34,5', '1,2,3', '1.234.5'])(
    'rechaza el formato inválido %j',
    (input) => {
      expect(parseArsAmount(input)).toBeNull()
    },
  )

  it('AC-62: acepta 999.999.999.999,99 y rechaza 1.000.000.000.000', () => {
    expect(parseArsAmount('999.999.999.999,99')).toBe('999999999999.99')
    expect(parseArsAmount('1.000.000.000.000')).toBeNull()
  })
})

describe('formatArs', () => {
  it('formatea en pesos con el formato es-AR', () => {
    expect(formatArs('1234.56')).toBe('$ 1.234,56')
  })

  it('formatea el monto máximo sin perder centavos', () => {
    expect(formatArs('999999999999.99')).toBe('$ 999.999.999.999,99')
  })
})
