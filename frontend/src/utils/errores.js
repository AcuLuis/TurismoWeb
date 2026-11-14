// Interpretación de errores de la API (ProblemDetails) para mostrarlos al usuario.

/** Mensaje legible para cualquier error de una petición. */
export function mensajeError(error, porDefecto = 'Ocurrió un error inesperado. Inténtalo nuevamente.') {
  if (!error) return porDefecto
  if (error.code === 'ECONNABORTED') return 'El servidor tardó demasiado en responder. Inténtalo nuevamente.'
  if (!error.response) {
    return 'No se pudo conectar con el servidor. Verifica tu conexión a internet e inténtalo nuevamente.'
  }
  const datos = error.response.data
  if (datos && typeof datos === 'object') {
    if (datos.detail) return datos.detail
    if (datos.title) return datos.title
  }
  return porDefecto
}

/**
 * Errores por campo de una respuesta 400 (ValidationProblemDetails).
 * Devuelve { campo: 'primer mensaje' } con las claves en camelCase.
 */
export function erroresDeCampo(error) {
  const errores = error?.response?.status === 400 ? error.response.data?.errors : null
  if (!errores) return {}
  return Object.fromEntries(
    Object.entries(errores).map(([campo, mensajes]) => [
      campo,
      Array.isArray(mensajes) ? mensajes[0] : String(mensajes),
    ]),
  )
}

export function estadoHttp(error) {
  return error?.response?.status ?? 0
}
