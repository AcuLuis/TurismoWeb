// Acceso a la API de TurismoGo agrupado por área. Cada función devuelve los datos de la respuesta.
import { api } from '@/boot/axios'

const datos = (promesa) => promesa.then((r) => r.data)

/** Elimina parámetros vacíos para no enviar "?q=&destino=". */
const limpiar = (params = {}) =>
  Object.fromEntries(
    Object.entries(params).filter(([, v]) => v !== null && v !== undefined && v !== ''),
  )

export const authApi = {
  login: (correo, password) => datos(api.post('/auth/login', { correo, password })),
  registrarTurista: (solicitud) => datos(api.post('/auth/registro/turista', solicitud)),
  registrarEmpresa: (solicitud) => datos(api.post('/auth/registro/empresa', solicitud)),
  me: () => datos(api.get('/auth/me')),
}

export const cuentaApi = {
  actualizarPerfil: (solicitud) => datos(api.put('/cuenta/perfil', solicitud)),
  cambiarPassword: (passwordActual, passwordNueva) =>
    datos(api.put('/cuenta/password', { passwordActual, passwordNueva })),
}

export const catalogoApi = {
  buscar: (filtros) => datos(api.get('/actividades', { params: limpiar(filtros) })),
  detalle: (id) => datos(api.get(`/actividades/${id}`)),
  resenas: (id, pagina = 1) => datos(api.get(`/actividades/${id}/resenas`, { params: { pagina } })),
  destinos: () => datos(api.get('/destinos')),
}

export const turistaApi = {
  reservar: (actividadId, cantidadPersonas, notas) =>
    datos(api.post('/reservas', { actividadId, cantidadPersonas, notas: notas || null })),
  misReservas: (filtro, pagina = 1) => datos(api.get('/mis/reservas', { params: limpiar({ filtro, pagina }) })),
  cancelar: (id, motivo) => datos(api.post(`/reservas/${id}/cancelar`, { motivo: motivo || null })),
  resenar: (actividadId, valoracion, comentario) =>
    datos(api.post(`/actividades/${actividadId}/resenas`, { valoracion, comentario })),
}

export const empresaApi = {
  perfil: () => datos(api.get('/empresa/perfil')),
  actualizarPerfil: (solicitud) => datos(api.put('/empresa/perfil', solicitud)),
  panel: () => datos(api.get('/empresa/panel')),
  actividades: (params) => datos(api.get('/empresa/actividades', { params: limpiar(params) })),
  actividad: (id) => datos(api.get(`/empresa/actividades/${id}`)),
  crear: (solicitud) => datos(api.post('/empresa/actividades', solicitud)),
  actualizar: (id, solicitud) => datos(api.put(`/empresa/actividades/${id}`, solicitud)),
  publicar: (id) => datos(api.post(`/empresa/actividades/${id}/publicar`)),
  despublicar: (id) => datos(api.post(`/empresa/actividades/${id}/despublicar`)),
  cancelar: (id, motivo) => datos(api.post(`/empresa/actividades/${id}/cancelar`, { motivo })),
  duplicar: (id) => datos(api.post(`/empresa/actividades/${id}/duplicar`)),
  eliminar: (id) => datos(api.delete(`/empresa/actividades/${id}`)),
  reservas: (params) => datos(api.get('/empresa/reservas', { params: limpiar(params) })),
  confirmar: (id) => datos(api.post(`/empresa/reservas/${id}/confirmar`)),
  rechazar: (id, motivo) => datos(api.post(`/empresa/reservas/${id}/rechazar`, { motivo })),
  /** Descarga el CSV de participantes como archivo. */
  async descargarParticipantes(id) {
    const r = await api.get(`/empresa/actividades/${id}/participantes.csv`, { responseType: 'blob' })
    const disposicion = r.headers['content-disposition'] || ''
    const nombre = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposicion)?.[1] || 'participantes.csv'
    const enlace = document.createElement('a')
    enlace.href = URL.createObjectURL(r.data)
    enlace.download = decodeURIComponent(nombre)
    enlace.click()
    URL.revokeObjectURL(enlace.href)
  },
}

export const adminApi = {
  resumen: () => datos(api.get('/admin/resumen')),
  empresas: (params) => datos(api.get('/admin/empresas', { params: limpiar(params) })),
  verificarEmpresa: (id) => datos(api.post(`/admin/empresas/${id}/verificar`)),
  suspenderEmpresa: (id, motivo) => datos(api.post(`/admin/empresas/${id}/suspender`, { motivo })),
  usuarios: (params) => datos(api.get('/admin/usuarios', { params: limpiar(params) })),
  suspenderUsuario: (id) => datos(api.post(`/admin/usuarios/${id}/suspender`)),
  reactivarUsuario: (id) => datos(api.post(`/admin/usuarios/${id}/reactivar`)),
  resenas: (params) => datos(api.get('/admin/resenas', { params: limpiar(params) })),
  ocultarResena: (id, motivo) => datos(api.post(`/admin/resenas/${id}/ocultar`, { motivo })),
  mostrarResena: (id) => datos(api.post(`/admin/resenas/${id}/mostrar`)),
}
