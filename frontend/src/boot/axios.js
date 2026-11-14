import { defineBoot } from '#q-app'
import axios from 'axios'
import { Notify } from 'quasar'

// URL del backend: variable TG_API_URL (archivo .env o entorno). Por defecto, la API local.
export const API_URL = (import.meta.env.TG_API_URL || 'http://localhost:5259').replace(/\/$/, '')

// Única instancia HTTP de la aplicación.
const api = axios.create({
  baseURL: `${API_URL}/api`,
  timeout: 15000,
})

export default defineBoot(({ app, router, store }) => {
  // Import diferido para evitar dependencias circulares (el store usa los servicios que usan "api").
  let authStore = null
  const obtenerAuth = async () => {
    if (!authStore) {
      const { useAuthStore } = await import('@/stores/auth')
      authStore = useAuthStore(store)
    }
    return authStore
  }

  api.interceptors.request.use(async (config) => {
    const auth = await obtenerAuth()
    if (auth.token) config.headers.Authorization = `Bearer ${auth.token}`
    return config
  })

  api.interceptors.response.use(
    (respuesta) => respuesta,
    async (error) => {
      const auth = await obtenerAuth()
      const esLogin = error.config?.url?.startsWith('/auth/login')
      // Sesión vencida o cuenta suspendida: se cierra la sesión y se pide ingresar de nuevo.
      if (error.response?.status === 401 && auth.token && !esLogin) {
        auth.cerrarSesion()
        Notify.create({ type: 'warning', message: 'Tu sesión expiró. Ingresa nuevamente para continuar.' })
        const actual = router.currentRoute.value
        if (actual.meta.requiereSesion) {
          router.replace({ path: '/ingresar', query: { redirect: actual.fullPath } })
        }
      }
      return Promise.reject(error)
    },
  )

  app.config.globalProperties.$api = api
})

export { api }
