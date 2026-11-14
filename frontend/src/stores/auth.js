import { defineStore, acceptHMRUpdate } from 'pinia'
import { authApi } from '@/services/api'

const CLAVE = 'tg.sesion'

/** Ruta de inicio según el rol. */
export function inicioPorRol(rol) {
  if (rol === 'Empresa') return '/empresa'
  if (rol === 'Admin') return '/admin'
  return '/'
}

function leerSesionGuardada() {
  try {
    const sesion = JSON.parse(localStorage.getItem(CLAVE) || 'null')
    if (!sesion?.token || !sesion?.expiraEn) return null
    // Se descarta si el token ya expiró
    if (new Date(sesion.expiraEn).getTime() <= Date.now()) {
      localStorage.removeItem(CLAVE)
      return null
    }
    return sesion
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('auth', {
  state: () => {
    const guardada = leerSesionGuardada()
    return {
      token: guardada?.token ?? null,
      expiraEn: guardada?.expiraEn ?? null,
      usuario: guardada?.usuario ?? null,
      verificada: false,
    }
  },

  getters: {
    autenticado: (s) => !!s.token && !!s.usuario,
    rol: (s) => s.usuario?.rol ?? null,
    esTurista: (s) => s.usuario?.rol === 'Turista',
    esEmpresa: (s) => s.usuario?.rol === 'Empresa',
    esAdmin: (s) => s.usuario?.rol === 'Admin',
    empresa: (s) => s.usuario?.empresa ?? null,
    nombreCorto: (s) => s.usuario?.nombre?.split(' ')[0] ?? '',
    rutaInicio: (s) => inicioPorRol(s.usuario?.rol),
  },

  actions: {
    guardar(sesion) {
      this.token = sesion.token
      this.expiraEn = sesion.expiraEn
      this.usuario = sesion.usuario
      this.verificada = true
      this.persistir()
    },

    persistir() {
      try {
        localStorage.setItem(
          CLAVE,
          JSON.stringify({ token: this.token, expiraEn: this.expiraEn, usuario: this.usuario }),
        )
      } catch {
        // El almacenamiento puede estar bloqueado (modo privado): la sesión dura mientras la pestaña esté abierta.
      }
    },

    async login(correo, password) {
      this.guardar(await authApi.login(correo, password))
    },

    async registrarTurista(solicitud) {
      this.guardar(await authApi.registrarTurista(solicitud))
    },

    async registrarEmpresa(solicitud) {
      this.guardar(await authApi.registrarEmpresa(solicitud))
    },

    /** Valida la sesión guardada contra el servidor (una vez por carga de la aplicación). */
    async verificarSesion() {
      if (!this.token || this.verificada) return
      try {
        this.usuario = await authApi.me()
        this.persistir()
      } catch (error) {
        if (error?.response?.status === 401) this.cerrarSesion()
      } finally {
        this.verificada = true
      }
    },

    actualizarUsuario(usuario) {
      this.usuario = usuario
      this.persistir()
    },

    cerrarSesion() {
      this.token = null
      this.expiraEn = null
      this.usuario = null
      try {
        localStorage.removeItem(CLAVE)
      } catch {
        /* sin almacenamiento */
      }
    },
  },
})

if (import.meta.hot) {
  import.meta.hot.accept(acceptHMRUpdate(useAuthStore, import.meta.hot))
}
