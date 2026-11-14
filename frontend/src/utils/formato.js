// Formateo de fechas, moneda y textos para la interfaz (locale es-PE).

const LOCALE = 'es-PE'

/**
 * Convierte una fecha "YYYY-MM-DD" (sin hora) en un Date LOCAL.
 * Importante: new Date('2026-12-01') se interpreta como UTC y en Perú (UTC-5) se mostraría el día anterior.
 */
export function parsearFecha(valor) {
  if (!valor) return null
  if (valor instanceof Date) return valor
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(String(valor).slice(0, 10))
  if (!m) return new Date(valor)
  return new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]))
}

/** Fecha local en formato "YYYY-MM-DD". */
export function aFechaIso(fecha) {
  const d = parsearFecha(fecha)
  if (!d) return ''
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${d.getFullYear()}-${mm}-${dd}`
}

export function hoyIso() {
  return aFechaIso(new Date())
}

export function sumarDias(fecha, dias) {
  const d = parsearFecha(fecha)
  return aFechaIso(new Date(d.getFullYear(), d.getMonth(), d.getDate() + dias))
}

/** "12 dic 2026" (corto) o "sábado, 12 de diciembre de 2026" (largo). */
export function formatFecha(valor, estilo = 'corto') {
  const d = parsearFecha(valor)
  if (!d || Number.isNaN(d.getTime())) return ''
  const opciones =
    estilo === 'largo'
      ? { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }
      : { day: 'numeric', month: 'short', year: 'numeric' }
  return new Intl.DateTimeFormat(LOCALE, opciones).format(d).replace('.', '')
}

/** "12 – 15 dic 2026", "28 dic 2026 – 2 ene 2027" o "12 dic 2026" si es el mismo día. */
export function formatRangoFechas(inicio, fin) {
  const a = parsearFecha(inicio)
  const b = parsearFecha(fin)
  if (!a) return ''
  if (!b || a.getTime() === b.getTime()) return formatFecha(a)
  const mismoAnio = a.getFullYear() === b.getFullYear()
  const mismoMes = mismoAnio && a.getMonth() === b.getMonth()
  if (mismoMes) {
    const mes = new Intl.DateTimeFormat(LOCALE, { month: 'short' }).format(b).replace('.', '').toLowerCase()
    return `${a.getDate()} – ${b.getDate()} ${mes} ${b.getFullYear()}`
  }
  if (mismoAnio) {
    const f = new Intl.DateTimeFormat(LOCALE, { day: 'numeric', month: 'short' })
    return `${f.format(a).replace('.', '')} – ${formatFecha(b)}`
  }
  return `${formatFecha(a)} – ${formatFecha(b)}`
}

/** Fecha y hora (para marcas de tiempo UTC del servidor). */
export function formatFechaHora(valorUtc) {
  if (!valorUtc) return ''
  const texto = String(valorUtc)
  const d = new Date(/[zZ]|[+-]\d{2}:\d{2}$/.test(texto) ? texto : `${texto}Z`)
  return new Intl.DateTimeFormat(LOCALE, { dateStyle: 'medium', timeStyle: 'short' }).format(d)
}

/** Solo la fecha (formato corto) de una marca de tiempo UTC del servidor. */
export function formatFechaDeUtc(valorUtc) {
  if (!valorUtc) return ''
  const texto = String(valorUtc)
  return formatFecha(new Date(/[zZ]|[+-]\d{2}:\d{2}$/.test(texto) ? texto : `${texto}Z`))
}

const formatoSoles =new Intl.NumberFormat(LOCALE, {
  style: 'currency',
  currency: 'PEN',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

/** "S/ 1,250.00" */
export function formatMoneda(valor) {
  if (valor === null || valor === undefined || valor === '') return ''
  return formatoSoles.format(Number(valor))
}

export function pluralizar(cantidad, singular, plural = `${singular}s`) {
  return `${cantidad} ${cantidad === 1 ? singular : plural}`
}

export function duracionTexto(dias) {
  if (!dias || dias <= 1) return '1 día'
  return `${dias} días · ${dias - 1} ${dias - 1 === 1 ? 'noche' : 'noches'}`
}

export function iniciales(nombre = '', apellidos = '') {
  const a = nombre.trim().charAt(0)
  const b = apellidos.trim().charAt(0)
  return `${a}${b}`.toUpperCase() || '?'
}

/** Días que faltan desde hoy hasta la fecha indicada (negativo si ya pasó). */
export function diasHasta(valor) {
  const d = parsearFecha(valor)
  const h = parsearFecha(hoyIso())
  return Math.round((d - h) / 86400000)
}
