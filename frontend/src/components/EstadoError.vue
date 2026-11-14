<template>
  <EstadoVacio icono="cloud_off" :titulo="titulo" :mensaje="mensaje">
    <q-btn
      color="primary"
      unelevated
      no-caps
      icon="refresh"
      label="Reintentar"
      @click="$emit('reintentar')"
    />
  </EstadoVacio>
</template>

<script setup>
import EstadoVacio from '@/components/EstadoVacio.vue'

defineProps({
  titulo: { type: String, default: 'No pudimos cargar la información' },
  mensaje: { type: String, default: '' },
})
defineEmits(['reintentar'])
</script>
