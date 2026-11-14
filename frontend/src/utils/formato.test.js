import { describe, expect, it } from 'vitest'
import {
  aFechaIso,
  duracionTexto,
  formatFecha,
  formatMoneda,
  formatRangoFechas,
  iniciales,
  parsearFecha,
  pluralizar,
  sumarDias,
} from './formato'

describe('fechas', () => {
  it('no desplaza un día las fechas "YYYY-MM-DD" (error original en UTC-5)', () => {
    const d = parsearFecha('2026-12-01')
    expect(d.getDate()).toBe(1)
    expect(d.getMonth()).toBe(11)
    expect(formatFecha('2026-12-01')).toBe('1 dic 2026')
  })

  it('formatea rangos de forma compacta', () => {
    expect(formatRangoFechas('2026-12-05', '2026-12-08')).toBe('5 – 8 dic 2026')
    expect(formatRangoFechas('2026-12-05', '2026-12-05')).toBe('5 dic 2026')
    expect(formatRangoFechas('2026-12-30', '2027-01-02')).toBe('30 dic 2026 – 2 ene 2027')
  })

  it('suma días respetando el cambio de mes y año', () => {
    expect(sumarDias('2026-12-30', 3)).toBe('2027-01-02')
    expect(aFechaIso(new Date(2026, 0, 5))).toBe('2026-01-05')
  })
})

describe('textos', () => {
  it('muestra la moneda en soles (con espacio no separable entre símbolo y monto)', () => {
    expect(formatMoneda(650)).toBe('S/ 650.00')
    expect(formatMoneda(2450.5)).toBe('S/ 2,450.50')
    expect(formatMoneda(null)).toBe('')
  })

  it('pluraliza y describe la duración', () => {
    expect(pluralizar(1, 'cupo')).toBe('1 cupo')
    expect(pluralizar(3, 'cupo')).toBe('3 cupos')
    expect(duracionTexto(1)).toBe('1 día')
    expect(duracionTexto(4)).toBe('4 días · 3 noches')
    expect(duracionTexto(2)).toBe('2 días · 1 noche')
  })

  it('obtiene iniciales', () => {
    expect(iniciales('ana', 'quispe')).toBe('AQ')
    expect(iniciales('', '')).toBe('?')
  })
})
