<template>
  <q-page class="tg-contenedor q-py-lg">
    <EncabezadoPagina titulo="Mis actividades" subtitulo="Crea, publica y administra tus salidas.">
      <template #acciones>
        <q-btn unelevated no-caps color="primary" icon="add" label="Nueva actividad" to="/empresa/actividades/nueva" />
      </template>
    </EncabezadoPagina>

    <EstadoEmpresaBanner :estado="auth.empresa?.estado" :motivo="auth.empresa?.motivoEstado" />

    <div class="row q-col-gutter-sm q-mb-md">
      <q-input v-model="busqueda" class="col-12 col-sm-7" outlined dense clearable debounce="400" bg-color="white" placeholder="Buscar por nombre o destino" aria-label="Buscar actividades">
        <template #prepend><q-icon name="search" /></template>
      </q-input>
      <q-select v-model="estado" class="col-12 col-sm-5" :options="opcionesEstado" emit-value map-options outlined dense bg-color="white" label="Estado" />
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
      :rows-per-page-options="[10, 20, 50]"
      no-data-label="No hay actividades con estos filtros."
      loading-label="Cargando…"
      rows-per-page-label="Filas por página"
      :pagination-label="(a, b, total) => `${a}-${b} de ${total}`"
      @request="alPaginar"
    >
      <template #no-data>
        <EstadoVacio class="full-width" icono="event_note" titulo="No hay actividades para mostrar" :mensaje="busqueda || estado ? 'Prueba con otros filtros.' : 'Crea tu primera actividad para empezar.'">
          <q-btn v-if="!busqueda && !estado" unelevated no-caps color="primary" label="Crear actividad" to="/empresa/actividades/nueva" />
        </EstadoVacio>
      </template>

      <template #body-cell-nombre="p">
        <q-td :props="p">
          <router-link :to="`/empresa/actividades/${p.row.id}/editar`" class="text-weight-medium enlace-fila">{{ p.row.nombre }}</router-link>
          <div class="text-caption tg-texto-suave">{{ p.row.destino }}</div>
        </q-td>
      </template>
      <template #body-cell-ocupacion="p">
        <q-td :props="p"><OcupacionBarra :reservados="p.row.cuposReservados" :capacidad="p.row.capacidad" /></q-td>
      </template>
      <template #body-cell-estado="p">
        <q-td :props="p">
          <EstadoActividadChip :estado="p.row.estado" :finalizada="p.row.finalizada" />
          <q-badge v-if="p.row.reservasPendientes" color="secondary" class="q-ml-xs" :label="p.row.reservasPendientes" :title="`${p.row.reservasPendientes} reservas por confirmar`" />
        </q-td>
      </template>
      <template #body-cell-acciones="p">
        <q-td :props="p" auto-width>
          <MenuAcciones :fila="p.row" @accion="ejecutar" />
        </q-td>
      </template>

      <!-- Vista de tarjetas en móvil -->
      <template #item="p">
        <div class="col-12 q-pa-xs">
          <div class="tg-tarjeta q-pa-md">
            <div class="row items-start no-wrap">
              <div class="col">
                <router-link :to="`/empresa/actividades/${p.row.id}/editar`" class="text-weight-medium enlace-fila">{{ p.row.nombre }}</router-link>
                <div class="text-caption tg-texto-suave">{{ p.row.destino }} · {{ formatRangoFechas(p.row.fechaInicio, p.row.fechaFin) }}</div>
              </div>
              <MenuAcciones :fila="p.row" @accion="ejecutar" />
            </div>
            <div class="row items-center justify-between q-mt-sm q-gutter-sm">
              <EstadoActividadChip :estado="p.row.estado" :finalizada="p.row.finalizada" />
              <span class="text-weight-medium">{{ formatMoneda(p.row.precio) }}</span>
            </div>
            <OcupacionBarra class="q-mt-sm" :reservados="p.row.cuposReservados" :capacidad="p.row.capacidad" />
          </div>
        </div>
      </template>
    </q-table>
  </q-page>
</template>

<script setup>
import { defineComponent, h, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { QBtn, QItem, QItemSection, QIcon, QList, QMenu, QSeparator } from 'quasar'
import EncabezadoPagina from '@/components/EncabezadoPagina.vue'
import EstadoActividadChip from '@/components/EstadoActividadChip.vue'
import EstadoEmpresaBanner from '@/components/EstadoEmpresaBanner.vue'
import EstadoError from '@/components/EstadoError.vue'
import EstadoVacio from '@/components/EstadoVacio.vue'
import OcupacionBarra from '@/components/OcupacionBarra.vue'
import { useAvisos } from '@/composables/useAvisos'
import { empresaApi } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { mensajeError } from '@/utils/errores'
import { formatMoneda, formatRangoFechas } from '@/utils/formato'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const avisos = useAvisos()

const opcionesEstado = [
  { label: 'Todos los estados', value: null },
  { label: 'Publicadas', value: 'Publicada' },
  { label: 'Borradores', value: 'Borrador' },
  { label: 'Canceladas', value: 'Cancelada' },
]

const columnas = [
  { name: 'nombre', label: 'Actividad', field: 'nombre', align: 'left' },
  { name: 'fechas', label: 'Fechas', field: (r) => formatRangoFechas(r.fechaInicio, r.fechaFin), align: 'left' },
  { name: 'precio', label: 'Precio', field: 'precio', format: formatMoneda, align: 'right' },
  { name: 'ocupacion', label: 'Ocupación', field: 'cuposReservados', align: 'left' },
  { name: 'estado', label: 'Estado', field: 'estado', align: 'left' },
  { name: 'acciones', label: '', field: 'id', align: 'right' },
]

const busqueda = ref('')
const estado = ref(['Publicada', 'Borrador', 'Cancelada'].includes(route.query.estado) ? route.query.estado : null)
const filas = ref([])
const cargando = ref(true)
const error = ref('')
const paginacion = ref({ page: 1, rowsPerPage: 20, rowsNumber: 0 })

async function cargar() {
  cargando.value = true
  error.value = ''
  try {
    const r = await empresaApi.actividades({
      q: busqueda.value,
      estado: estado.value,
      pagina: paginacion.value.page,
      tamano: paginacion.value.rowsPerPage,
    })
    filas.value = r.items
    paginacion.value.rowsNumber = r.total
  } catch (e) {
    error.value = mensajeError(e)
  } finally {
    cargando.value = false
  }
}

function alPaginar({ pagination }) {
  paginacion.value = { ...paginacion.value, page: pagination.page, rowsPerPage: pagination.rowsPerPage }
  cargar()
}

watch([busqueda, estado], () => {
  paginacion.value.page = 1
  router.replace({ query: estado.value ? { estado: estado.value } : {} })
  cargar()
})

async function ejecutar(accion, fila) {
  try {
    if (accion === 'editar') return router.push(`/empresa/actividades/${fila.id}/editar`)
    if (accion === 'ver') return router.push(`/actividades/${fila.id}`)
    if (accion === 'reservas') return router.push(`/empresa/reservas?actividadId=${fila.id}`)
    if (accion === 'participantes') return await empresaApi.descargarParticipantes(fila.id)
    if (accion === 'publicar') {
      await empresaApi.publicar(fila.id)
      avisos.exito('¡Actividad publicada! Ya aparece en el catálogo.')
    }
    if (accion === 'despublicar') {
      if (!(await avisos.confirmar({ titulo: 'Retirar del catálogo', mensaje: `"${fila.nombre}" volverá a ser un borrador y dejará de recibir reservas.`, textoAceptar: 'Retirar' }))) return
      await empresaApi.despublicar(fila.id)
      avisos.exito('La actividad se retiró del catálogo.')
    }
    if (accion === 'duplicar') {
      const copia = await empresaApi.duplicar(fila.id)
      avisos.exito('Se creó una copia como borrador. Revisa sus fechas antes de publicarla.')
      return router.push(`/empresa/actividades/${copia.id}/editar`)
    }
    if (accion === 'cancelar') {
      const motivo = await avisos.pedirMotivo({
        titulo: 'Cancelar actividad',
        mensaje: `Se cancelarán todas las reservas activas de "${fila.nombre}" y los viajeros verán el motivo. Esta acción no se puede deshacer.`,
        textoAceptar: 'Cancelar actividad',
      })
      if (!motivo) return
      await empresaApi.cancelar(fila.id, motivo)
      avisos.exito('La actividad fue cancelada y se notificó el motivo en las reservas.')
    }
    if (accion === 'eliminar') {
      if (!(await avisos.confirmar({ titulo: 'Eliminar borrador', mensaje: `¿Eliminar "${fila.nombre}" definitivamente?`, textoAceptar: 'Eliminar', peligro: true }))) return
      await empresaApi.eliminar(fila.id)
      avisos.exito('Borrador eliminado.')
    }
    cargar()
  } catch (e) {
    avisos.error(e)
  }
}

/** Menú de acciones según el estado de la actividad. */
const MenuAcciones = defineComponent({
  props: { fila: { type: Object, required: true } },
  emits: ['accion'],
  setup(props, { emit }) {
    const item = (accion, icono, texto, color) =>
      h(QItem, { clickable: true, onClick: () => emit('accion', accion, props.fila) }, () => [
        h(QItemSection, { avatar: true }, () => h(QIcon, { name: icono, color })),
        h(QItemSection, () => texto),
      ])
    return () => {
      const f = props.fila
      const vigente = !f.finalizada && f.estado !== 'Cancelada'
      const opciones = [
        vigente && item('editar', 'edit', 'Editar'),
        item('ver', 'visibility', f.estado === 'Publicada' ? 'Ver en el catálogo' : 'Vista previa'),
        f.estado === 'Borrador' && vigente && item('publicar', 'public', 'Publicar', 'positive'),
        f.estado === 'Publicada' && vigente && item('despublicar', 'unpublished', 'Retirar del catálogo'),
        f.cuposReservados > 0 && item('reservas', 'groups', 'Ver reservas'),
        f.cuposReservados > 0 && item('participantes', 'download', 'Descargar participantes (CSV)'),
        item('duplicar', 'content_copy', 'Duplicar'),
        vigente && h(QSeparator),
        vigente && item('cancelar', 'event_busy', 'Cancelar actividad', 'negative'),
        f.estado === 'Borrador' && item('eliminar', 'delete', 'Eliminar borrador', 'negative'),
      ].filter(Boolean)
      return h(QBtn, { flat: true, round: true, dense: true, icon: 'more_vert', 'aria-label': `Acciones para ${f.nombre}` }, () =>
        h(QMenu, { anchor: 'bottom right', self: 'top right', autoClose: true }, () => h(QList, { style: 'min-width: 240px' }, () => opciones)),
      )
    }
  },
})

cargar()
</script>

<style scoped>
.enlace-fila {
  color: inherit;
  text-decoration: none;
}

.enlace-fila:hover {
  color: var(--q-primary);
  text-decoration: underline;
}
</style>
