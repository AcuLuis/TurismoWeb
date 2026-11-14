<template>
  <router-link :to="`/actividades/${actividad.id}`" class="tarjeta tg-tarjeta" :aria-label="etiquetaAccesible">
    <ActividadPortada :destino="actividad.destino" :imagen-url="actividad.imagenUrl" :alt="actividad.nombre" alto="170px">
      <q-badge v-if="pocosCupos" class="tarjeta__aviso" color="secondary" text-color="white">
        {{ actividad.cuposDisponibles === 1 ? '¡Queda 1 cupo!' : `¡Quedan ${actividad.cuposDisponibles} cupos!` }}
      </q-badge>
    </ActividadPortada>

    <div class="tarjeta__cuerpo">
      <div class="row items-center q-gutter-x-sm tg-texto-suave text-caption">
        <span><q-icon name="place" size="16px" aria-hidden="true" /> {{ actividad.destino }}</span>
        <span aria-hidden="true">·</span>
        <span>{{ duracionCorta }}</span>
      </div>

      <h3 class="tarjeta__titulo">{{ actividad.nombre }}</h3>

      <div class="tarjeta__empresa text-body2">
        <span class="ellipsis">{{ actividad.empresa.nombreComercial }}</span>
        <ValoracionEstrellas
          v-if="actividad.valoracion"
          :valor="actividad.valoracion"
          :total="actividad.totalResenas"
          tamano="16px"
        />
        <ValoracionEstrellas
          v-else-if="actividad.empresa.valoracion"
          :valor="actividad.empresa.valoracion"
          :total="actividad.empresa.totalResenas"
          tamano="16px"
        />
      </div>

      <div class="tarjeta__pie">
        <div class="text-body2">
          <q-icon name="event" size="16px" aria-hidden="true" />
          {{ formatRangoFechas(actividad.fechaInicio, actividad.fechaFin) }}
        </div>
        <div class="text-right">
          <div class="tarjeta__precio">{{ formatMoneda(actividad.precio) }}</div>
          <div class="text-caption tg-texto-suave">por persona</div>
        </div>
      </div>
    </div>
  </router-link>
</template>

<script setup>
import { computed } from 'vue'
import ActividadPortada from '@/components/ActividadPortada.vue'
import ValoracionEstrellas from '@/components/ValoracionEstrellas.vue'
import { formatMoneda, formatRangoFechas } from '@/utils/formato'

const props = defineProps({
  actividad: { type: Object, required: true },
})

const pocosCupos = computed(() => props.actividad.cuposDisponibles > 0 && props.actividad.cuposDisponibles <= 5)
const duracionCorta = computed(() =>
  props.actividad.duracionDias > 1 ? `${props.actividad.duracionDias} días` : 'Full day',
)
const etiquetaAccesible = computed(
  () =>
    `${props.actividad.nombre}, ${props.actividad.destino}, ${formatRangoFechas(props.actividad.fechaInicio, props.actividad.fechaFin)}, ${formatMoneda(props.actividad.precio)} por persona`,
)
</script>

<style scoped lang="scss">
.tarjeta {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  text-decoration: none;
  color: inherit;
  transition:
    box-shadow 0.2s ease,
    transform 0.2s ease;

  &:hover {
    box-shadow: 0 10px 24px rgba(15, 23, 42, 0.1);
    transform: translateY(-2px);
  }
}

.tarjeta__aviso {
  position: absolute;
  top: 12px;
  left: 12px;
  padding: 4px 8px;
  font-weight: 600;
}

.tarjeta__cuerpo {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 14px 16px 16px;
  flex: 1;
}

.tarjeta__titulo {
  font-size: 1.08rem;
  line-height: 1.35;
  font-weight: 600;
  margin: 2px 0 0;
}

.tarjeta__empresa {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
  color: var(--tg-texto-suave);
}

.tarjeta__pie {
  margin-top: auto;
  padding-top: 10px;
  border-top: 1px solid var(--tg-borde);
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  gap: 8px;
}

.tarjeta__precio {
  font-size: 1.15rem;
  font-weight: 700;
  color: $primary;
}
</style>
