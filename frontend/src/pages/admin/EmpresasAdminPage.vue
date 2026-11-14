<template>
  <q-page class="tg-contenedor q-py-lg">
    <EncabezadoPagina titulo="Empresas" subtitulo="Verifica el RUC y los datos de contacto antes de habilitar la publicación." />

    <div class="row q-col-gutter-sm q-mb-md">
      <q-input v-model="q" class="col-12 col-sm-7" outlined dense clearable debounce="400" bg-color="white" placeholder="Nombre, razón social, RUC o correo" aria-label="Buscar empresas">
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
      :rows-per-page-options="[20]"
      :pagination-label="(a, b, t) => `${a}-${b} de ${t}`"
      @request="alPaginar"
    >
      <template #no-data><EstadoVacio class="full-width" icono="storefront" titulo="No hay empresas con estos filtros" /></template>
      <template #body-cell-empresa="p">
        <q-td :props="p" class="celda-ajustable">
          <div class="text-weight-medium">{{ p.row.nombreComercial }}</div>
          <div class="text-caption tg-texto-suave">{{ p.row.razonSocial }} · RUC {{ p.row.ruc }}</div>
        </q-td>
      </template>
      <template #body-cell-contacto="p">
        <q-td :props="p" class="celda-ajustable">
          <div>{{ p.row.representante }}</div>
          <div class="text-caption"><a :href="`mailto:${p.row.correo}`">{{ p.row.correo }}</a> · {{ p.row.telefono }}</div>
        </q-td>
      </template>
      <template #body-cell-estado="p">
        <q-td :props="p">
          <q-chip dense square :color="colores[p.row.estado].fondo" :text-color="colores[p.row.estado].texto" :icon="colores[p.row.estado].icono" class="q-ma-none">{{ p.row.estado }}</q-chip>
          <div v-if="p.row.motivoEstado" class="text-caption tg-texto-suave">{{ p.row.motivoEstado }}</div>
        </q-td>
      </template>
      <template #body-cell-acciones="p">
        <q-td :props="p" auto-width>
          <AccionesEmpresa :fila="p.row" @verificar="verificar" @suspender="suspender" />
        </q-td>
      </template>
      <template #item="p">
        <div class="col-12 q-pa-xs">
          <div class="tg-tarjeta q-pa-md">
            <div class="row items-start justify-between no-wrap">
              <div>
                <div class="text-weight-medium">{{ p.row.nombreComercial }}</div>
                <div class="text-caption tg-texto-suave">RUC {{ p.row.ruc }} · {{ p.row.correo }}</div>
              </div>
              <q-chip dense square :color="colores[p.row.estado].fondo" :text-color="colores[p.row.estado].texto" class="q-ma-none">{{ p.row.estado }}</q-chip>
            </div>
            <div class="q-mt-sm"><AccionesEmpresa :fila="p.row" @verificar="verificar" @suspender="suspender" /></div>
          </div>
        </div>
      </template>
    </q-table>
  </q-page>
</template>

<script setup>
import { defineComponent, h, ref, watch } from 'vue'
import { QBtn } from 'quasar'
import EncabezadoPagina from '@/components/EncabezadoPagina.vue'
import EstadoError from '@/components/EstadoError.vue'
import EstadoVacio from '@/components/EstadoVacio.vue'
import { useAvisos } from '@/composables/useAvisos'
import { useTablaServidor } from '@/composables/useTablaServidor'
import { adminApi } from '@/services/api'
import { formatFechaDeUtc } from '@/utils/formato'

const avisos = useAvisos()
const opcionesEstado = [
  { label: 'Todos', value: null },
  { label: 'Pendientes', value: 'Pendiente' },
  { label: 'Verificadas', value: 'Verificada' },
  { label: 'Suspendidas', value: 'Suspendida' },
]
const colores = {
  Pendiente: { fondo: 'amber-2', texto: 'brown-10', icono: 'hourglass_top' },
  Verificada: { fondo: 'green-1', texto: 'green-10', icono: 'verified' },
  Suspendida: { fondo: 'red-1', texto: 'red-10', icono: 'block' },
}
const columnas = [
  { name: 'empresa', label: 'Empresa', field: 'nombreComercial', align: 'left' },
  { name: 'contacto', label: 'Representante', field: 'representante', align: 'left' },
  { name: 'publicadas', label: 'Publicadas', field: 'actividadesPublicadas', align: 'center' },
  { name: 'registro', label: 'Registro', field: 'creadoEn', format: (v) => formatFechaDeUtc(v), align: 'left' },
  { name: 'estado', label: 'Estado', field: 'estado', align: 'left' },
  { name: 'acciones', label: '', field: 'id', align: 'right' },
]

const q = ref('')
const estado = ref(null)
const { filas, cargando, error, paginacion, cargar, alPaginar, reiniciar } = useTablaServidor((pagina) =>
  adminApi.empresas({ q: q.value, estado: estado.value, pagina }),
)
watch([q, estado], reiniciar)

async function verificar(fila) {
  const ok = await avisos.confirmar({
    titulo: `Verificar ${fila.nombreComercial}`,
    mensaje: `Confirma que revisaste el RUC ${fila.ruc} y los datos de contacto. La empresa podrá publicar actividades.`,
    textoAceptar: 'Verificar empresa',
  })
  if (!ok) return
  try {
    await adminApi.verificarEmpresa(fila.id)
    avisos.exito(`${fila.nombreComercial} fue verificada.`)
    cargar()
  } catch (e) {
    avisos.error(e)
  }
}

async function suspender(fila) {
  const motivo = await avisos.pedirMotivo({
    titulo: `Suspender ${fila.nombreComercial}`,
    mensaje: 'Sus actividades dejarán de mostrarse en el catálogo. La empresa verá el motivo.',
    textoAceptar: 'Suspender',
  })
  if (!motivo) return
  try {
    await adminApi.suspenderEmpresa(fila.id, motivo)
    avisos.exito(`${fila.nombreComercial} fue suspendida.`)
    cargar()
  } catch (e) {
    avisos.error(e)
  }
}

const AccionesEmpresa = defineComponent({
  props: { fila: { type: Object, required: true } },
  emits: ['verificar', 'suspender'],
  setup(props, { emit }) {
    return () =>
      h('div', { class: 'row no-wrap q-gutter-xs' }, [
        props.fila.estado !== 'Verificada' &&
          h(QBtn, { unelevated: true, dense: true, noCaps: true, color: 'positive', label: 'Verificar', onClick: () => emit('verificar', props.fila) }),
        props.fila.estado !== 'Suspendida' &&
          h(QBtn, { flat: true, dense: true, noCaps: true, color: 'negative', label: 'Suspender', onClick: () => emit('suspender', props.fila) }),
      ])
  },
})

cargar()
</script>

<style scoped>
.celda-ajustable {
  white-space: normal;
  min-width: 180px;
}
</style>
