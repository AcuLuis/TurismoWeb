<template>
  <q-page class="tg-contenedor q-py-lg">
    <EncabezadoPagina titulo="Explorar actividades" subtitulo="Encuentra tu próxima aventura por destino, fecha y presupuesto." />

    <div class="catalogo">
      <!-- Filtros (escritorio) -->
      <aside v-if="$q.screen.gt.sm" class="catalogo__filtros tg-tarjeta q-pa-md" aria-label="Filtros">
        <FiltrosForm v-model="borrador" :destinos="destinos" @aplicar="aplicar" @limpiar="limpiar" />
      </aside>

      <section class="catalogo__resultados" aria-labelledby="titulo-resultados">
        <div class="barra">
          <q-input
            v-model="texto"
            outlined
            dense
            clearable
            bg-color="white"
            placeholder="Buscar por nombre, destino o empresa"
            class="barra__busqueda"
            aria-label="Buscar actividades"
            debounce="400"
            @update:model-value="buscarTexto"
          >
            <template #prepend><q-icon name="search" /></template>
          </q-input>
          <q-btn
            v-if="$q.screen.lt.md"
            outline
            no-caps
            color="primary"
            icon="tune"
            :label="filtrosActivos.length ? `Filtros (${filtrosActivos.length})` : 'Filtros'"
            @click="dialogoFiltros = true"
          />
          <q-select
            :model-value="filtros.orden"
            :options="opcionesOrden"
            emit-value
            map-options
            outlined
            dense
            bg-color="white"
            label="Ordenar por"
            class="barra__orden"
            @update:model-value="cambiarOrden"
          />
        </div>

        <div v-if="filtrosActivos.length" class="row items-center q-gutter-xs q-mb-sm">
          <q-chip
            v-for="f in filtrosActivos"
            :key="f.clave"
            removable
            color="teal-1"
            text-color="teal-10"
            :aria-label="`Quitar filtro ${f.etiqueta}`"
            @remove="quitarFiltro(f.claves)"
          >
            {{ f.etiqueta }}
          </q-chip>
          <q-btn flat dense no-caps color="primary" label="Limpiar todo" @click="limpiar" />
        </div>

        <h2 id="titulo-resultados" class="text-body1 tg-texto-suave q-my-sm" aria-live="polite">
          <template v-if="cargando">Buscando actividades…</template>
          <template v-else-if="!error">{{ textoResultados }}</template>
        </h2>

        <EstadoError v-if="error" :mensaje="error" @reintentar="cargar" />
        <EstadoVacio
          v-else-if="!cargando && resultado.items.length === 0"
          icono="search_off"
          titulo="No encontramos actividades con esos filtros"
          mensaje="Prueba con otras fechas, otro destino o un rango de precios más amplio."
        >
          <q-btn unelevated no-caps color="primary" label="Limpiar filtros" @click="limpiar" />
        </EstadoVacio>

        <div v-else class="grilla">
          <template v-if="cargando">
            <ActividadCardSkeleton v-for="n in 6" :key="n" />
          </template>
          <ActividadCard v-for="a in resultado.items" v-else :key="a.id" :actividad="a" />
        </div>

        <div v-if="!cargando && resultado.totalPaginas > 1" class="row justify-center q-mt-lg">
          <q-pagination
            :model-value="resultado.pagina"
            :max="resultado.totalPaginas"
            :max-pages="6"
            direction-links
            boundary-links
            color="primary"
            @update:model-value="cambiarPagina"
          />
        </div>
      </section>
    </div>

    <!-- Filtros (móvil) -->
    <q-dialog v-model="dialogoFiltros" position="bottom">
      <q-card class="dialogo-filtros">
        <q-card-section class="row items-center">
          <div class="text-h6">Filtros</div>
          <q-space />
          <q-btn v-close-popup flat round dense icon="close" aria-label="Cerrar filtros" />
        </q-card-section>
        <q-card-section class="q-pt-none">
          <FiltrosForm v-model="borrador" :destinos="destinos" @aplicar="aplicar(); dialogoFiltros = false" @limpiar="limpiar(); dialogoFiltros = false" />
        </q-card-section>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ActividadCard from '@/components/ActividadCard.vue'
import ActividadCardSkeleton from '@/components/ActividadCardSkeleton.vue'
import EncabezadoPagina from '@/components/EncabezadoPagina.vue'
import EstadoError from '@/components/EstadoError.vue'
import EstadoVacio from '@/components/EstadoVacio.vue'
import FiltrosForm from '@/components/FiltrosCatalogo.vue'
import { catalogoApi } from '@/services/api'
import { mensajeError } from '@/utils/errores'
import { formatFecha, formatMoneda, pluralizar } from '@/utils/formato'

const route = useRoute()
const router = useRouter()

const opcionesOrden = [
  { label: 'Próximas fechas', value: 'fecha' },
  { label: 'Menor precio', value: 'precio-asc' },
  { label: 'Mayor precio', value: 'precio-desc' },
  { label: 'Mejor valoradas', value: 'valoracion' },
]

// Los filtros viven en la URL: se pueden compartir y se conservan al volver atrás.
const filtros = computed(() => ({
  q: route.query.q || '',
  destino: route.query.destino || null,
  desde: route.query.desde || '',
  hasta: route.query.hasta || '',
  precioMin: route.query.precioMin || '',
  precioMax: route.query.precioMax || '',
  orden: route.query.orden || 'fecha',
  pagina: Number(route.query.pagina) || 1,
}))

const borrador = reactive({ destino: null, desde: '', hasta: '', precioMin: '', precioMax: '' })
const texto = ref(filtros.value.q)
const destinos = ref([])
const resultado = ref({ items: [], total: 0, pagina: 1, totalPaginas: 0 })
const cargando = ref(true)
const error = ref('')
const dialogoFiltros = ref(false)

const textoResultados = computed(() =>
  resultado.value.total === 0 ? '' : `${pluralizar(resultado.value.total, 'actividad', 'actividades')} disponibles`,
)

const filtrosActivos = computed(() => {
  const f = filtros.value
  const lista = []
  if (f.q) lista.push({ clave: 'q', claves: ['q'], etiqueta: `“${f.q}”` })
  if (f.destino) lista.push({ clave: 'destino', claves: ['destino'], etiqueta: f.destino })
  if (f.desde || f.hasta) {
    const etiqueta = f.desde && f.hasta ? `${formatFecha(f.desde)} – ${formatFecha(f.hasta)}` : f.desde ? `Desde ${formatFecha(f.desde)}` : `Hasta ${formatFecha(f.hasta)}`
    lista.push({ clave: 'fechas', claves: ['desde', 'hasta'], etiqueta })
  }
  if (f.precioMin || f.precioMax) {
    const etiqueta = f.precioMin && f.precioMax ? `${formatMoneda(f.precioMin)} – ${formatMoneda(f.precioMax)}` : f.precioMin ? `Desde ${formatMoneda(f.precioMin)}` : `Hasta ${formatMoneda(f.precioMax)}`
    lista.push({ clave: 'precio', claves: ['precioMin', 'precioMax'], etiqueta })
  }
  return lista
})

function sincronizarBorrador() {
  Object.assign(borrador, {
    destino: filtros.value.destino,
    desde: filtros.value.desde,
    hasta: filtros.value.hasta,
    precioMin: filtros.value.precioMin,
    precioMax: filtros.value.precioMax,
  })
  texto.value = filtros.value.q
}

function navegar(cambios) {
  const query = { ...route.query, ...cambios }
  Object.keys(query).forEach((k) => (query[k] === '' || query[k] === null || query[k] === undefined) && delete query[k])
  if (!('pagina' in cambios)) delete query.pagina
  if (query.orden === 'fecha') delete query.orden
  router.push({ query })
}

const aplicar = () => navegar({ ...borrador })
const buscarTexto = (v) => navegar({ q: (v || '').trim() })
const cambiarOrden = (orden) => navegar({ orden })
const cambiarPagina = (pagina) => navegar({ pagina: pagina > 1 ? pagina : null })
const quitarFiltro = (claves) => navegar(Object.fromEntries(claves.map((c) => [c, null])))
const limpiar = () => router.push({ query: {} })

async function cargar() {
  cargando.value = true
  error.value = ''
  try {
    resultado.value = await catalogoApi.buscar({ ...filtros.value, tamano: 12 })
  } catch (e) {
    error.value = mensajeError(e)
  } finally {
    cargando.value = false
  }
}

watch(
  () => route.query,
  () => {
    sincronizarBorrador()
    cargar()
  },
)

onMounted(async () => {
  sincronizarBorrador()
  cargar()
  try {
    destinos.value = await catalogoApi.destinos()
  } catch {
    destinos.value = []
  }
})
</script>

<style scoped lang="scss">
.catalogo {
  display: grid;
  grid-template-columns: 280px 1fr;
  gap: 24px;
  align-items: start;

  @media (max-width: 1023px) {
    grid-template-columns: 1fr;
  }
}

.catalogo__filtros {
  position: sticky;
  top: 84px;
}

.catalogo__resultados {
  min-width: 0;
}

.barra {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-bottom: 12px;
}

.barra__busqueda {
  flex: 1 1 260px;
}

.barra__orden {
  flex: 0 0 200px;

  @media (max-width: 599px) {
    flex: 1 1 100%;
  }
}

.grilla {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 20px;
}

.dialogo-filtros {
  width: 100%;
  border-radius: 20px 20px 0 0;
}
</style>
