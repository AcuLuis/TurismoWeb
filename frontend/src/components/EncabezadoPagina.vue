<template>
  <header class="encabezado">
    <div class="col">
      <q-breadcrumbs v-if="migas.length" class="q-mb-xs text-caption" active-color="grey-8">
        <q-breadcrumbs-el v-for="m in migas" :key="m.label" :label="m.label" :to="m.to" />
      </q-breadcrumbs>
      <h1 class="tg-titulo-pagina">{{ titulo }}</h1>
      <p v-if="subtitulo" class="tg-subtitulo">{{ subtitulo }}</p>
    </div>
    <div v-if="$slots.acciones" class="encabezado__acciones">
      <slot name="acciones" />
    </div>
  </header>
</template>

<script setup>
defineProps({
  titulo: { type: String, required: true },
  subtitulo: { type: String, default: '' },
  migas: { type: Array, default: () => [] },
})
</script>

<style scoped>
.encabezado {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  justify-content: space-between;
  gap: 12px 16px;
  margin-bottom: 20px;
}

.encabezado__acciones {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
</style>
