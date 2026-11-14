import { defineBoot } from '#q-app'
import axios from 'axios'

// URL del backend: variable TG_API_URL (archivo .env o entorno). Por defecto, la API local.
export const API_URL = (import.meta.env.TG_API_URL || 'http://localhost:5259').replace(/\/$/, '')

// Única instancia HTTP de la aplicación.
const api = axios.create({
  baseURL: `${API_URL}/api`,
  timeout: 15000,
})

export default defineBoot(({ app }) => {
  app.config.globalProperties.$api = api
})

export { api }
