<template>
  <div class="ocupacion" :aria-label="`${reservados} de ${capacidad} cupos reservados`">
    <div class="row justify-between text-caption">
      <span>{{ reservados }}/{{ capacidad }}</span>
      <span class="tg-texto-suave">{{ Math.round(proporcion * 100) }}%</span>
    </div>
    <q-linear-progress :value="proporcion" :color="color" track-color="grey-3" rounded size="6px" />
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  reservados: { type: Number, required: true },
  capacidad: { type: Number, required: true },
})

const proporcion = computed(() => (props.capacidad ? Math.min(1, props.reservados / props.capacidad) : 0))
const color = computed(() => (proporcion.value >= 1 ? 'secondary' : proporcion.value >= 0.7 ? 'warning' : 'primary'))
</script>

<style scoped>
.ocupacion {
  min-width: 110px;
}
</style>
