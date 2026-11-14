<template>
  <q-page class="tg-contenedor q-py-lg">
    <EncabezadoPagina titulo="Usuarios" subtitulo="Una cuenta suspendida pierde el acceso de inmediato." />

    <div class="row q-col-gutter-sm q-mb-md">
      <q-input v-model="q" class="col-12 col-sm-7" outlined dense clearable debounce="400" bg-color="white" placeholder="Nombre, correo o DNI" aria-label="Buscar usuarios">
        <template #prepend><q-icon name="search" /></template>
      </q-input>
      <q-select v-model="rol" class="col-12 col-sm-5" :options="opcionesRol" emit-value map-options outlined dense bg-color="white" label="Rol" />
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
      <template #no-data><EstadoVacio class="full-width" icono="person_search" titulo="No hay usuarios con estos filtros" /></template>
      <template #body-cell-nombre="p">
        <q-td :props="p">
          <div class="text-weight-medium">{{ p.row.nombre }}</div>
          <div class="text-caption tg-texto-suave">{{ p.row.correo }}<template v-if="p.row.dni"> · DNI {{ p.row.dni }}</template></div>
        </q-td>
      </template>
      <template #body-cell-rol="p">
        <q-td :props="p">
          {{ etiquetasRol[p.row.rol] }}<div v-if="p.row.empresa" class="text-caption tg-texto-suave">{{ p.row.empresa }}</div>
        </q-td>
      </template>
      <template #body-cell-estado="p">
        <q-td :props="p">
          <q-chip dense square :color="p.row.activo ? 'green-1' : 'red-1'" :text-color="p.row.activo ? 'green-10' : 'red-10'" :icon="p.row.activo ? 'check_circle' : 'block'" class="q-ma-none">
            {{ p.row.activo ? 'Activa' : 'Suspendida' }}
          </q-chip>
        </q-td>
      </template>
      <template #body-cell-acciones="p">
        <q-td :props="p" auto-width>
          <template v-if="p.row.rol !== 'Admin'">
            <q-btn v-if="p.row.activo" flat dense no-caps color="negative" label="Suspender" @click="cambiar(p.row, false)" />
            <q-btn v-else flat dense no-caps color="positive" label="Reactivar" @click="cambiar(p.row, true)" />
          </template>
        </q-td>
      </template>
      <template #item="p">
        <div class="col-12 q-pa-xs">
          <div class="tg-tarjeta q-pa-md row items-center no-wrap">
            <div class="col">
              <div class="text-weight-medium">{{ p.row.nombre }}</div>
              <div class="text-caption tg-texto-suave">{{ etiquetasRol[p.row.rol] }} · {{ p.row.correo }}</div>
            </div>
            <template v-if="p.row.rol !== 'Admin'">
              <q-btn v-if="p.row.activo" flat dense no-caps color="negative" label="Suspender" @click="cambiar(p.row, false)" />
              <q-btn v-else flat dense no-caps color="positive" label="Reactivar" @click="cambiar(p.row, true)" />
            </template>
          </div>
        </div>
      </template>
    </q-table>
  </q-page>
</template>

<script setup>
import { ref, watch } from 'vue'
import EncabezadoPagina from '@/components/EncabezadoPagina.vue'
import EstadoError from '@/components/EstadoError.vue'
import EstadoVacio from '@/components/EstadoVacio.vue'
import { useAvisos } from '@/composables/useAvisos'
import { useTablaServidor } from '@/composables/useTablaServidor'
import { adminApi } from '@/services/api'
import { formatFechaDeUtc, formatFechaHora } from '@/utils/formato'

const avisos = useAvisos()
const etiquetasRol = { Turista: 'Viajero', Empresa: 'Empresa', Admin: 'Administrador' }
const opcionesRol = [
  { label: 'Todos', value: null },
  { label: 'Viajeros', value: 'Turista' },
  { label: 'Empresas', value: 'Empresa' },
  { label: 'Administradores', value: 'Admin' },
]
const columnas = [
  { name: 'nombre', label: 'Usuario', field: 'nombre', align: 'left' },
  { name: 'rol', label: 'Rol', field: 'rol', align: 'left' },
  { name: 'registro', label: 'Registro', field: 'creadoEn', format: (v) => formatFechaDeUtc(v), align: 'left' },
  { name: 'acceso', label: 'Último acceso', field: 'ultimoAccesoEn', format: (v) => (v ? formatFechaHora(v) : 'Nunca'), align: 'left' },
  { name: 'estado', label: 'Estado', field: 'activo', align: 'left' },
  { name: 'acciones', label: '', field: 'id', align: 'right' },
]

const q = ref('')
const rol = ref(null)
const { filas, cargando, error, paginacion, cargar, alPaginar, reiniciar } = useTablaServidor((pagina) =>
  adminApi.usuarios({ q: q.value, rol: rol.value, pagina }),
)
watch([q, rol], reiniciar)

async function cambiar(fila, activo) {
  const ok = await avisos.confirmar({
    titulo: activo ? 'Reactivar cuenta' : 'Suspender cuenta',
    mensaje: activo
      ? `${fila.nombre} podrá volver a ingresar.`
      : `${fila.nombre} perderá el acceso de inmediato. Sus reservas existentes se mantienen.`,
    textoAceptar: activo ? 'Reactivar' : 'Suspender',
    peligro: !activo,
  })
  if (!ok) return
  try {
    activo ? await adminApi.reactivarUsuario(fila.id) : await adminApi.suspenderUsuario(fila.id)
    avisos.exito(activo ? 'Cuenta reactivada.' : 'Cuenta suspendida.')
    cargar()
  } catch (e) {
    avisos.error(e)
  }
}

cargar()
</script>
