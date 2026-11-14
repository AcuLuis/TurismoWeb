import { describe, expect, it } from 'vitest'
import { erroresDeCampo, mensajeError } from './errores'

describe('interpretación de errores de la API', () => {
  it('usa el detalle de ProblemDetails', () => {
    const error = { response: { status: 409, data: { title: 'Conflicto', detail: 'Ya existe una cuenta con ese DNI.' } } }
    expect(mensajeError(error)).toBe('Ya existe una cuenta con ese DNI.')
  })

  it('explica la falta de conexión', () => {
    expect(mensajeError({ message: 'Network Error' })).toMatch(/No se pudo conectar/)
  })

  it('extrae el primer error por campo solo en respuestas 400', () => {
    const error = { response: { status: 400, data: { errors: { dni: ['El DNI debe tener 8 dígitos.'], 'itinerario[0].titulo': ['Requerido'] } } } }
    expect(erroresDeCampo(error)).toEqual({ dni: 'El DNI debe tener 8 dígitos.', 'itinerario[0].titulo': 'Requerido' })
    expect(erroresDeCampo({ response: { status: 500, data: {} } })).toEqual({})
  })
})
