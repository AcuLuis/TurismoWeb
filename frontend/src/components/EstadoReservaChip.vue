<template>
  <q-chip
    dense
    square
    :color="config.color"
    :text-color="config.texto"
    :icon="config.icono"
    class="text-weight-medium q-ma-none"
  >
    {{ config.etiqueta }}
  </q-chip>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  estado: { type: String, required: true },
})

const configuraciones = {
  Pendiente: { etiqueta: 'Pendiente', color: 'amber-2', texto: 'brown-10', icono: 'schedule' },
  Confirmada: { etiqueta: 'Confirmada', color: 'green-1', texto: 'green-10', icono: 'check_circle' },
  Completada: { etiqueta: 'Completada', color: 'blue-1', texto: 'blue-10', icono: 'flag' },
  Rechazada: { etiqueta: 'Rechazada', color: 'red-1', texto: 'red-10', icono: 'block' },
  Cancelada: { etiqueta: 'Cancelada', color: 'grey-3', texto: 'grey-9', icono: 'cancel' },
}

const config = computed(() => configuraciones[props.estado] ?? configuraciones.Cancelada)
</script>
