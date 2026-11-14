<template>
  <div class="estado" role="status">
    <div class="estado__icono" aria-hidden="true">
      <q-icon :name="icono" size="40px" />
    </div>
    <h2 class="estado__titulo">{{ titulo }}</h2>
    <p v-if="mensaje" class="estado__mensaje">{{ mensaje }}</p>
    <div v-if="$slots.default" class="q-mt-md row justify-center q-gutter-sm">
      <slot />
    </div>
  </div>
</template>

<script setup>
defineProps({
  icono: { type: String, default: 'travel_explore' },
  titulo: { type: String, required: true },
  mensaje: { type: String, default: '' },
})
</script>

<style scoped lang="scss">
.estado {
  text-align: center;
  padding: 40px 16px;
  max-width: 460px;
  margin: 0 auto;
}

.estado__icono {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 76px;
  height: 76px;
  border-radius: 50%;
  background: rgba(15, 118, 110, 0.1);
  color: $primary;
  margin-bottom: 12px;
}

.estado__titulo {
  font-size: 1.2rem;
  font-weight: 600;
  margin: 0;
  line-height: 1.4;
}

.estado__mensaje {
  color: var(--tg-texto-suave);
  margin: 6px 0 0;
}
</style>
