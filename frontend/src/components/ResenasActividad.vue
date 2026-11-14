<template>
  <section aria-labelledby="titulo-resenas">
    <h2 id="titulo-resenas" class="seccion-titulo">Reseñas de viajeros</h2>

    <div v-if="cargando" class="q-gutter-y-sm">
      <q-skeleton type="text" width="40%" />
      <q-skeleton height="80px" />
    </div>
    <p v-else-if="error" class="text-negative">
      {{ error }} <q-btn flat dense no-caps color="primary" label="Reintentar" @click="cargar(1)" />
    </p>
    <p v-else-if="datos.total === 0" class="tg-texto-suave">
      Esta salida aún no tiene reseñas. Las opiniones las escriben viajeros que completaron la actividad.
    </p>

    <template v-else>
      <div class="resumen">
        <div class="resumen__promedio">
          <div class="text-h3 text-weight-bold">{{ datos.promedio.toFixed(1) }}</div>
          <q-rating :model-value="datos.promedio" readonly size="20px" color="warning" icon="star_border" icon-selected="star" icon-half="star_half" :aria-label="`Promedio ${datos.promedio} de 5`" />
          <div class="text-caption tg-texto-suave">{{ pluralizar(datos.total, 'reseña') }}</div>
        </div>
        <ul class="resumen__barras" aria-label="Distribución de valoraciones">
          <li v-for="estrellas in [5, 4, 3, 2, 1]" :key="estrellas">
            <span class="resumen__etiqueta">{{ estrellas }} <q-icon name="star" size="14px" aria-hidden="true" /></span>
            <q-linear-progress :value="datos.distribucion[estrellas] / datos.total" color="warning" track-color="grey-3" rounded size="8px" class="col" />
            <span class="resumen__cantidad">{{ datos.distribucion[estrellas] }}</span>
          </li>
        </ul>
      </div>

      <ul class="lista">
        <li v-for="r in items" :key="r.id" class="resena">
          <div class="row items-center q-gutter-x-sm">
            <q-avatar size="34px" color="teal-1" text-color="primary" class="text-weight-medium">{{ r.autor.charAt(0) }}</q-avatar>
            <div>
              <div class="text-weight-medium">{{ r.autor }}</div>
              <div class="text-caption tg-texto-suave">{{ formatFechaDeUtc(r.fecha) }}</div>
            </div>
            <q-space />
            <q-rating :model-value="r.valoracion" readonly size="16px" color="warning" icon="star_border" icon-selected="star" :aria-label="`${r.valoracion} de 5 estrellas`" />
          </div>
          <p class="q-mt-sm q-mb-none">{{ r.comentario }}</p>
        </li>
      </ul>

      <q-btn v-if="pagina < datos.totalPaginas" outline no-caps color="primary" label="Ver más reseñas" :loading="cargandoMas" @click="cargar(pagina + 1)" />
    </template>
  </section>
</template>

<script setup>
import { onMounted, ref, watch } from 'vue'
import { catalogoApi } from '@/services/api'
import { mensajeError } from '@/utils/errores'
import { formatFechaDeUtc, pluralizar } from '@/utils/formato'

const props = defineProps({ actividadId: { type: Number, required: true } })

const datos = ref({ total: 0, promedio: 0, distribucion: {}, totalPaginas: 0 })
const items = ref([])
const pagina = ref(1)
const cargando = ref(true)
const cargandoMas = ref(false)
const error = ref('')

async function cargar(p) {
  p === 1 ? (cargando.value = true) : (cargandoMas.value = true)
  error.value = ''
  try {
    const r = await catalogoApi.resenas(props.actividadId, p)
    datos.value = r
    items.value = p === 1 ? r.items : [...items.value, ...r.items]
    pagina.value = p
  } catch (e) {
    error.value = mensajeError(e)
  } finally {
    cargando.value = false
    cargandoMas.value = false
  }
}

onMounted(() => cargar(1))
watch(() => props.actividadId, () => cargar(1))
</script>

<style scoped lang="scss">
.resumen {
  display: flex;
  flex-wrap: wrap;
  gap: 24px;
  align-items: center;
  padding: 16px;
  border-radius: 14px;
  background: #fff;
  border: 1px solid var(--tg-borde);
  margin-bottom: 16px;
}

.resumen__promedio {
  text-align: center;
  min-width: 120px;
}

.resumen__barras {
  list-style: none;
  padding: 0;
  margin: 0;
  flex: 1 1 240px;
  display: grid;
  gap: 6px;

  li {
    display: flex;
    align-items: center;
    gap: 10px;
  }
}

.resumen__etiqueta {
  width: 34px;
  white-space: nowrap;
}

.resumen__cantidad {
  width: 24px;
  text-align: right;
  color: var(--tg-texto-suave);
}

.lista {
  list-style: none;
  padding: 0;
  margin: 0 0 16px;
}

.resena {
  padding: 16px 0;
  border-bottom: 1px solid var(--tg-borde);
}
</style>
