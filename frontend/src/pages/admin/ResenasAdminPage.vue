<template>
  <q-page class="tg-contenedor tg-contenedor--estrecho q-py-lg">
    <EncabezadoPagina titulo="Moderación de reseñas" subtitulo="Las reseñas con menor valoración aparecen primero. Ocultar no las elimina." />

    <div class="row q-col-gutter-sm q-mb-md">
      <q-input v-model="q" class="col-12 col-sm-7" outlined dense clearable debounce="400" bg-color="white" placeholder="Texto, actividad o correo" aria-label="Buscar reseñas">
        <template #prepend><q-icon name="search" /></template>
      </q-input>
      <q-select v-model="oculta" class="col-12 col-sm-5" :options="opciones" emit-value map-options outlined dense bg-color="white" label="Visibilidad" />
    </div>

    <EstadoError v-if="error" :mensaje="error" @reintentar="cargar" />
    <div v-else-if="cargando" class="q-gutter-y-md"><q-skeleton v-for="n in 3" :key="n" height="120px" class="rounded-borders" /></div>
    <EstadoVacio v-else-if="!filas.length" icono="reviews" titulo="No hay reseñas con estos filtros" />

    <ul v-else class="lista">
      <li v-for="r in filas" :key="r.id" class="tg-tarjeta q-pa-md" :class="{ 'resena--oculta': r.oculta }">
        <div class="row items-start justify-between no-wrap q-gutter-sm">
          <div>
            <q-rating :model-value="r.valoracion" readonly size="18px" color="warning" icon="star_border" icon-selected="star" :aria-label="`${r.valoracion} de 5 estrellas`" />
            <div class="text-caption tg-texto-suave">
              {{ r.autor }} ({{ r.correo }}) · {{ formatFechaDeUtc(r.creadoEn) }}
            </div>
          </div>
          <q-chip v-if="r.oculta" dense square color="grey-3" text-color="grey-9" icon="visibility_off" class="q-ma-none">Oculta</q-chip>
        </div>
        <p class="q-my-sm">{{ r.comentario }}</p>
        <div class="text-caption">
          <router-link :to="`/actividades/${r.actividadId}`">{{ r.actividad }}</router-link> · {{ r.empresa }}
        </div>
        <div v-if="r.oculta && r.motivoOcultamiento" class="text-caption text-negative q-mt-xs">Motivo: {{ r.motivoOcultamiento }}</div>
        <div class="row justify-end q-mt-sm">
          <q-btn v-if="!r.oculta" flat dense no-caps color="negative" icon="visibility_off" label="Ocultar" @click="ocultar(r)" />
          <q-btn v-else flat dense no-caps color="primary" icon="visibility" label="Volver a mostrar" @click="mostrar(r)" />
        </div>
      </li>
    </ul>

    <div v-if="!cargando && paginacion.rowsNumber > paginacion.rowsPerPage" class="row justify-center q-mt-lg">
      <q-pagination
        :model-value="paginacion.page"
        :max="Math.ceil(paginacion.rowsNumber / paginacion.rowsPerPage)"
        direction-links
        color="primary"
        @update:model-value="(p) => alPaginar({ pagination: { page: p } })"
      />
    </div>
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
import { formatFechaDeUtc } from '@/utils/formato'

const avisos = useAvisos()
const opciones = [
  { label: 'Todas', value: null },
  { label: 'Visibles', value: false },
  { label: 'Ocultas', value: true },
]
const q = ref('')
const oculta = ref(null)
const { filas, cargando, error, paginacion, cargar, alPaginar, reiniciar } = useTablaServidor((pagina) =>
  adminApi.resenas({ q: q.value, oculta: oculta.value, pagina }),
)
watch([q, oculta], reiniciar)

async function ocultar(r) {
  const motivo = await avisos.pedirMotivo({
    titulo: 'Ocultar reseña',
    mensaje: 'La reseña dejará de mostrarse y de contar para el promedio.',
    textoAceptar: 'Ocultar',
  })
  if (!motivo) return
  try {
    await adminApi.ocultarResena(r.id, motivo)
    avisos.exito('Reseña oculta.')
    cargar()
  } catch (e) {
    avisos.error(e)
  }
}

async function mostrar(r) {
  try {
    await adminApi.mostrarResena(r.id)
    avisos.exito('La reseña vuelve a ser visible.')
    cargar()
  } catch (e) {
    avisos.error(e)
  }
}

cargar()
</script>

<style scoped>
.lista {
  list-style: none;
  padding: 0;
  margin: 0;
  display: grid;
  gap: 12px;
}

.resena--oculta {
  background: #f8fafc;
  opacity: 0.85;
}
</style>
