<template>
  <q-page class="tg-contenedor q-py-lg">
    <EncabezadoPagina titulo="Reservas recibidas" subtitulo="Confirma o rechaza reservas y descarga la lista de participantes.">
      <template v-if="filtros.actividadId" #acciones>
        <q-btn outline no-caps color="primary" icon="download" label="Participantes (CSV)" :loading="descargando" @click="descargar" />
      </template>
    </EncabezadoPagina>

    <div class="row q-col-gutter-sm q-mb-md">
      <q-select v-model="filtros.actividadId" class="col-12 col-md-5" :options="opcionesActividad" emit-value map-options outlined dense clearable bg-color="white" label="Actividad" />
      <q-select v-model="filtros.estado" class="col-6 col-md-3" :options="opcionesEstado" emit-value map-options outlined dense bg-color="white" label="Estado" />
      <q-input v-model="filtros.q" class="col-6 col-md-4" outlined dense clearable debounce="400" bg-color="white" placeholder="Código o viajero" aria-label="Buscar por código o nombre">
        <template #prepend><q-icon name="search" /></template>
      </q-input>
    </div>

    <EstadoError v-if="error" :mensaje="error" @reintentar="cargar" />

    <q-table
      v-else
      v-model:pagination="paginacion"
      :rows="filas"
      :columns="columnas"
      row-key="id"
      :loading="cargando"
      :grid="$q.screen.lt.md"
      flat
      bordered
      class="tg-tarjeta"
      :rows-per-page-options="[20]"
      loading-label="Cargando…"
      :pagination-label="(a, b, total) => `${a}-${b} de ${total}`"
      @request="alPaginar"
    >
      <template #no-data>
        <EstadoVacio class="full-width" icono="inbox" titulo="No hay reservas con estos filtros" mensaje="Cuando los viajeros reserven tus actividades aparecerán aquí." />
      </template>

      <template #body-cell-turista="p">
        <q-td :props="p" class="celda-ajustable">
          <div class="text-weight-medium">{{ p.row.turista }}</div>
          <div class="text-caption tg-texto-suave">DNI {{ p.row.dni || '—' }} · <a :href="`mailto:${p.row.correo}`">{{ p.row.correo }}</a><template v-if="p.row.telefono"> · <a :href="`tel:${p.row.telefono}`">{{ p.row.telefono }}</a></template></div>
        </q-td>
      </template>
      <template #body-cell-actividad="p">
        <q-td :props="p" class="celda-ajustable">
          <div>{{ p.row.actividad }}</div>
          <div class="text-caption tg-texto-suave">{{ formatFecha(p.row.fechaInicio) }}</div>
        </q-td>
      </template>
      <template #body-cell-estado="p">
        <q-td :props="p">
          <EstadoReservaChip :estado="p.row.estado" />
          <q-icon v-if="p.row.notas" name="sticky_note_2" color="accent" size="18px" class="q-ml-xs" :aria-label="`Nota: ${p.row.notas}`">
            <q-tooltip max-width="260px">{{ p.row.notas }}</q-tooltip>
          </q-icon>
          <div v-if="p.row.motivoEstado" class="text-caption tg-texto-suave ellipsis" style="max-width: 200px" :title="p.row.motivoEstado">{{ p.row.motivoEstado }}</div>
        </q-td>
      </template>
      <template #body-cell-acciones="p">
        <q-td :props="p" auto-width>
          <div v-if="p.row.estado === 'Pendiente'" class="row no-wrap q-gutter-xs">
            <q-btn unelevated round dense color="positive" icon="check" :aria-label="`Confirmar reserva ${p.row.codigo}`" :loading="procesando === p.row.id" @click="confirmar(p.row)">
              <q-tooltip>Confirmar</q-tooltip>
            </q-btn>
            <q-btn outline round dense color="negative" icon="close" :aria-label="`Rechazar reserva ${p.row.codigo}`" :disable="procesando === p.row.id" @click="rechazar(p.row)">
              <q-tooltip>Rechazar</q-tooltip>
            </q-btn>
          </div>
        </q-td>
      </template>

      <template #item="p">
        <div class="col-12 q-pa-xs">
          <div class="tg-tarjeta q-pa-md">
            <div class="row items-start justify-between no-wrap q-gutter-sm">
              <div>
                <div class="text-weight-medium">{{ p.row.turista }}</div>
                <div class="text-caption tg-texto-suave">{{ p.row.codigo }} · {{ p.row.actividad }} · {{ formatFecha(p.row.fechaInicio) }}</div>
              </div>
              <EstadoReservaChip :estado="p.row.estado" />
            </div>
            <div class="q-mt-sm text-body2">
              {{ pluralizar(p.row.cantidadPersonas, 'persona') }} · {{ formatMoneda(p.row.total) }}
              <template v-if="p.row.telefono"> · <a :href="`tel:${p.row.telefono}`">{{ p.row.telefono }}</a></template>
            </div>
            <div v-if="p.row.notas" class="text-caption q-mt-xs"><q-icon name="sticky_note_2" color="accent" /> {{ p.row.notas }}</div>
            <div v-if="p.row.estado === 'Pendiente'" class="row q-gutter-sm q-mt-sm">
              <q-btn unelevated no-caps color="positive" icon="check" label="Confirmar" :loading="procesando === p.row.id" @click="confirmar(p.row)" />
              <q-btn flat no-caps color="negative" label="Rechazar" @click="rechazar(p.row)" />
            </div>
          </div>
        </div>
      </template>
    </q-table>
  </q-page>
</template>

<script setup>
import { onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import EncabezadoPagina from '@/components/EncabezadoPagina.vue'
import EstadoError from '@/components/EstadoError.vue'
import EstadoReservaChip from '@/components/EstadoReservaChip.vue'
import EstadoVacio from '@/components/EstadoVacio.vue'
import { useAvisos } from '@/composables/useAvisos'
import { empresaApi } from '@/services/api'
import { mensajeError } from '@/utils/errores'
import { formatFecha, formatMoneda, formatRangoFechas, pluralizar } from '@/utils/formato'

const route = useRoute()
const router = useRouter()
const avisos = useAvisos()

const opcionesEstado = [
  { label: 'Todas', value: null },
  { label: 'Pendientes', value: 'Pendiente' },
  { label: 'Confirmadas', value: 'Confirmada' },
  { label: 'Rechazadas', value: 'Rechazada' },
  { label: 'Canceladas', value: 'Cancelada' },
]

const columnas = [
  { name: 'codigo', label: 'Código', field: 'codigo', align: 'left' },
  { name: 'turista', label: 'Viajero', field: 'turista', align: 'left' },
  { name: 'actividad', label: 'Actividad', field: 'actividad', align: 'left' },
  { name: 'personas', label: 'Personas', field: 'cantidadPersonas', align: 'center' },
  { name: 'total', label: 'Total', field: 'total', format: formatMoneda, align: 'right' },
  { name: 'estado', label: 'Estado', field: 'estado', align: 'left' },
  { name: 'acciones', label: '', field: 'id', align: 'right' },
]

const filtros = reactive({
  actividadId: Number(route.query.actividadId) || null,
  estado: opcionesEstado.some((o) => o.value === route.query.estado) ? route.query.estado : null,
  q: '',
})
const filas = ref([])
const opcionesActividad = ref([])
const cargando = ref(true)
const error = ref('')
const procesando = ref(null)
const descargando = ref(false)
const paginacion = ref({ page: 1, rowsPerPage: 20, rowsNumber: 0 })

async function cargar() {
  cargando.value = true
  error.value = ''
  try {
    const r = await empresaApi.reservas({ ...filtros, pagina: paginacion.value.page })
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

watch(
  () => ({ ...filtros }),
  () => {
    paginacion.value.page = 1
    const query = {}
    if (filtros.actividadId) query.actividadId = filtros.actividadId
    if (filtros.estado) query.estado = filtros.estado
    router.replace({ query })
    cargar()
  },
)

async function confirmar(fila) {
  procesando.value = fila.id
  try {
    await empresaApi.confirmar(fila.id)
    avisos.exito(`Reserva ${fila.codigo} confirmada.`)
    cargar()
  } catch (e) {
    avisos.error(e)
  } finally {
    procesando.value = null
  }
}

async function rechazar(fila) {
  const motivo = await avisos.pedirMotivo({
    titulo: `Rechazar reserva ${fila.codigo}`,
    mensaje: `El viajero ${fila.turista} verá el motivo y se liberarán ${pluralizar(fila.cantidadPersonas, 'cupo')}.`,
    textoAceptar: 'Rechazar reserva',
  })
  if (!motivo) return
  procesando.value = fila.id
  try {
    await empresaApi.rechazar(fila.id, motivo)
    avisos.exito('Reserva rechazada.')
    cargar()
  } catch (e) {
    avisos.error(e)
  } finally {
    procesando.value = null
  }
}

async function descargar() {
  descargando.value = true
  try {
    await empresaApi.descargarParticipantes(filtros.actividadId)
  } catch (e) {
    avisos.error(e)
  } finally {
    descargando.value = false
  }
}

onMounted(async () => {
  cargar()
  try {
    const r = await empresaApi.actividades({ tamano: 50 })
    opcionesActividad.value = r.items.map((a) => ({ label: `${a.nombre} · ${formatRangoFechas(a.fechaInicio, a.fechaFin)}`, value: a.id }))
  } catch {
    opcionesActividad.value = []
  }
})
</script>

<style scoped>
.celda-ajustable {
  white-space: normal;
  min-width: 180px;
}
</style>
