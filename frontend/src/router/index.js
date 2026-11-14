import { defineRouter } from '#q-app'
import {
  createRouter,
  createMemoryHistory,
  createWebHistory,
  createWebHashHistory,
} from 'vue-router'
import { Notify } from 'quasar'
import routes from './routes'
import { useAuthStore } from '@/stores/auth'

export default defineRouter(function ({ store }) {
  const createHistory = import.meta.env.QUASAR_SERVER
    ? createMemoryHistory
    : import.meta.env.QUASAR_VUE_ROUTER_MODE === 'history'
      ? createWebHistory
      : createWebHashHistory

  const Router = createRouter({
    // Al volver atrás se recupera la posición; al navegar a otra página se sube al inicio.
    scrollBehavior: (to, from, guardada) => guardada || (to.path !== from.path ? { left: 0, top: 0 } : false),
    routes,
    history: createHistory(import.meta.env.QUASAR_VUE_ROUTER_BASE),
  })

  // Control de acceso por sesión y rol
  Router.beforeEach(async (to) => {
    const auth = useAuthStore(store)
    await auth.verificarSesion()

    if (to.meta.requiereSesion && !auth.autenticado) {
      return { path: '/ingresar', query: { redirect: to.fullPath } }
    }
    if (to.meta.soloInvitados && auth.autenticado) {
      return auth.rutaInicio
    }
    if (to.meta.roles && !to.meta.roles.includes(auth.rol)) {
      Notify.create({ type: 'warning', message: 'No tienes acceso a esa sección.' })
      return auth.rutaInicio
    }
    return true
  })

  Router.afterEach((to) => {
    document.title = to.meta.titulo ? `${to.meta.titulo} | TurismoGo` : 'TurismoGo'
  })

  return Router
})
