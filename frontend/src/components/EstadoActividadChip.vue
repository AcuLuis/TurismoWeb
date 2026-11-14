<template>
  <q-chip dense square :color="c.color" :text-color="c.texto" :icon="c.icono" class="text-weight-medium q-ma-none">
    {{ c.etiqueta }}
  </q-chip>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  estado: { type: String, required: true },
  finalizada: { type: Boolean, default: false },
})

const c = computed(() => {
  if (props.finalizada && props.estado !== 'Cancelada')
    return { etiqueta: 'Finalizada', color: 'blue-1', texto: 'blue-10', icono: 'flag' }
  return (
    {
      Borrador: { etiqueta: 'Borrador', color: 'grey-3', texto: 'grey-9', icono: 'edit_note' },
      Publicada: { etiqueta: 'Publicada', color: 'green-1', texto: 'green-10', icono: 'public' },
      Cancelada: { etiqueta: 'Cancelada', color: 'red-1', texto: 'red-10', icono: 'event_busy' },
    }[props.estado] ?? { etiqueta: props.estado, color: 'grey-3', texto: 'grey-9', icono: 'help' }
  )
})
</script>
