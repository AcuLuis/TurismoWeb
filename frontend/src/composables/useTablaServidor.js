import { ref } from 'vue'
import { mensajeError } from '@/utils/errores'

/**
 * Estado común de una tabla con paginación en el servidor.
 * @param {(pagina: number) => Promise<{items, total}>} consultar
 */
export function useTablaServidor(consultar, filasPorPagina = 20) {
  const filas = ref([])
  const cargando = ref(true)
  const error = ref('')
  const paginacion = ref({ page: 1, rowsPerPage: filasPorPagina, rowsNumber: 0 })

  async function cargar() {
    cargando.value = true
    error.value = ''
    try {
      const r = await consultar(paginacion.value.page)
      filas.value = r.items
      paginacion.value.rowsNumber = r.total
    } catch (e) {
      error.value = mensajeError(e)
    } finally {
      cargando.value = false
    }
  }

  function alPaginar({ pagination }) {
    paginacion.value.page = pagination.page
    cargar()
  }

  function reiniciar() {
    paginacion.value.page = 1
    cargar()
  }

  return { filas, cargando, error, paginacion, cargar, alPaginar, reiniciar }
}
