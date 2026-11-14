import { describe, expect, it } from 'vitest'
import { coincideCon, contrasenaSegura, correo, dni, fuerzaContrasena, requerido, ruc, telefono, url } from './reglas'

// Las reglas devuelven true o el mensaje de error (contrato de :rules en Quasar).
// El registro original usaba arreglos anidados y por eso nunca mostraba errores.
describe('reglas de validación', () => {
  it('requerido', () => {
    expect(requerido('Obligatorio')('')).toBe('Obligatorio')
    expect(requerido('Obligatorio')('   ')).toBe('Obligatorio')
    expect(requerido()('x')).toBe(true)
  })

  it('correo', () => {
    expect(correo('ana@correo.pe')).toBe(true)
    expect(correo('no-es-correo')).toBeTypeOf('string')
  })

  it('DNI y RUC peruanos', () => {
    expect(dni('45678912')).toBe(true)
    expect(dni('1234')).toBeTypeOf('string')
    expect(ruc('20601234567')).toBe(true)
    expect(ruc('99999999999')).toBeTypeOf('string')
  })

  it('teléfono opcional', () => {
    expect(telefono('')).toBe(true)
    expect(telefono('+51987654321')).toBe(true)
    expect(telefono('12ab')).toBeTypeOf('string')
  })

  it('contraseña segura y coincidencia', () => {
    expect(contrasenaSegura('corta1')).toBeTypeOf('string')
    expect(contrasenaSegura('solamenteletras')).toBeTypeOf('string')
    expect(contrasenaSegura('Segura2026')).toBe(true)
    expect(coincideCon(() => 'abc')('abc')).toBe(true)
    expect(coincideCon(() => 'abc')('abd')).toBe('Las contraseñas no coinciden.')
    expect(fuerzaContrasena('Segura2026!xyz')).toBe(4)
  })

  it('url http(s)', () => {
    expect(url('https://ejemplo.pe/img.jpg')).toBe(true)
    expect(url('ftp://x')).toBeTypeOf('string')
  })
})
