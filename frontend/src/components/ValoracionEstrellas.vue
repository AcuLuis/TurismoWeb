<template>
  <span v-if="valor" class="valoracion" :aria-label="etiqueta">
    <q-icon name="star" color="warning" :size="tamano" aria-hidden="true" />
    <strong>{{ valor.toFixed(1) }}</strong>
    <span v-if="total !== null" class="tg-texto-suave">({{ total }})</span>
    <span v-if="sufijo" class="tg-texto-suave">{{ sufijo }}</span>
  </span>
  <span v-else-if="mostrarSinResenas" class="valoracion tg-texto-suave">
    <q-icon name="star_outline" :size="tamano" aria-hidden="true" />
    Sin reseñas aún
  </span>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  valor: { type: Number, default: null },
  total: { type: Number, default: null },
  sufijo: { type: String, default: '' },
  tamano: { type: String, default: '18px' },
  mostrarSinResenas: { type: Boolean, default: false },
})

const etiqueta = computed(
  () => `Valoración ${props.valor?.toFixed(1)} de 5${props.total !== null ? `, ${props.total} reseñas` : ''}`,
)
</script>

<style scoped>
.valoracion {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}
</style>
