// Reglas de validación para componentes Quasar (:rules). Cada regla devuelve true o un mensaje de error.
// Reflejan las validaciones del backend para dar respuesta inmediata al usuario.

const vacio = (v) => v === null || v === undefined || String(v).trim() === ''

export const requerido =
  (mensaje = 'Este campo es obligatorio.') =>
  (v) =>
    !vacio(v) || mensaje

export const maxLongitud = (max) => (v) =>
  vacio(v) || String(v).length <= max || `Máximo ${max} caracteres.`

export const minLongitud = (min) => (v) =>
  vacio(v) || String(v).trim().length >= min || `Mínimo ${min} caracteres.`

export const correo = (v) =>
  vacio(v) || /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(String(v).trim()) || 'Ingresa un correo válido.'

export const dni = (v) => vacio(v) || /^\d{8}$/.test(String(v).trim()) || 'El DNI debe tener 8 dígitos.'

export const ruc = (v) =>
  vacio(v) ||
  /^(10|15|17|20)\d{9}$/.test(String(v).trim()) ||
  'El RUC debe tener 11 dígitos y empezar por 10, 15, 17 o 20.'

export const telefono = (v) =>
  vacio(v) || /^\+?\d{7,15}$/.test(String(v).trim()) || 'Ingresa un teléfono válido (7 a 15 dígitos).'

export const contrasenaSegura = (v) => {
  if (vacio(v)) return 'Ingresa una contraseña.'
  if (String(v).length < 8) return 'Usa al menos 8 caracteres.'
  if (!/[A-Za-zÁÉÍÓÚáéíóúÑñ]/.test(v) || !/\d/.test(v)) return 'Incluye letras y números.'
  return true
}

/** Compara con otro valor (por ejemplo, "repetir contraseña"). */
export const coincideCon =
  (obtenerOtro, mensaje = 'Las contraseñas no coinciden.') =>
  (v) =>
    v === obtenerOtro() || mensaje

export const numeroEntre = (min, max, mensaje) => (v) => {
  if (vacio(v)) return true
  const n = Number(v)
  return (!Number.isNaN(n) && n >= min && n <= max) || mensaje || `Debe estar entre ${min} y ${max}.`
}

export const url = (v) =>
  vacio(v) || /^https?:\/\/\S+$/i.test(String(v).trim()) || 'Ingresa una URL que empiece con http:// o https://.'

/** Nivel de seguridad de la contraseña (0-4) para mostrar un indicador. */
export function fuerzaContrasena(v = '') {
  let puntos = 0
  if (v.length >= 8) puntos++
  if (v.length >= 12) puntos++
  if (/[a-z]/.test(v) && /[A-Z]/.test(v)) puntos++
  if (/\d/.test(v) && /[^A-Za-z0-9]/.test(v)) puntos++
  return puntos
}
