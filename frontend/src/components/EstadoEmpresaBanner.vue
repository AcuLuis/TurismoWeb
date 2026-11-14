<template>
  <q-banner v-if="estado === 'Pendiente'" rounded class="bg-amber-1 q-mb-lg">
    <template #avatar><q-icon name="hourglass_top" color="brown-8" /></template>
    <strong>Tu empresa está en verificación.</strong>
    Nuestro equipo está revisando tus datos (RUC y contacto). Mientras tanto puedes crear tus actividades como
    borradores; podrás publicarlas apenas se complete la verificación.
  </q-banner>
  <q-banner v-else-if="estado === 'Suspendida'" rounded class="bg-red-1 text-red-10 q-mb-lg">
    <template #avatar><q-icon name="gpp_bad" color="negative" /></template>
    <strong>Tu empresa está suspendida.</strong>
    Tus actividades no se muestran en el catálogo.
    <template v-if="motivo"> Motivo: {{ motivo }}.</template>
    Comunícate con el equipo de TurismoGo para resolverlo.
  </q-banner>
</template>

<script setup>
defineProps({
  estado: { type: String, default: null },
  motivo: { type: String, default: null },
})
</script>
