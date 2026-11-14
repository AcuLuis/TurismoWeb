<template>
  <q-form class="column q-gutter-y-md" @submit="$emit('aplicar')">
    <q-select
      v-model="modelo.destino"
      :options="destinos"
      label="Destino"
      outlined
      dense
      clearable
      emit-value
    >
      <template #prepend><q-icon name="place" /></template>
      <template #no-option>
        <q-item><q-item-section class="tg-texto-suave">Sin destinos disponibles</q-item-section></q-item>
      </template>
    </q-select>

    <fieldset class="grupo">
      <legend class="tg-etiqueta">Fecha de inicio</legend>
      <div class="column q-gutter-y-sm">
        <q-input v-model="modelo.desde" type="date" label="Desde" stack-label outlined dense :min="hoy" />
        <q-input v-model="modelo.hasta" type="date" label="Hasta" stack-label outlined dense :min="modelo.desde || hoy" :rules="[reglaFechas]" />
      </div>
    </fieldset>

    <fieldset class="grupo">
      <legend class="tg-etiqueta">Precio por persona (S/)</legend>
      <div class="row q-col-gutter-sm">
        <q-input v-model="modelo.precioMin" class="col-6" type="number" min="0" label="Mínimo" outlined dense inputmode="decimal" />
        <q-input v-model="modelo.precioMax" class="col-6" type="number" min="0" label="Máximo" outlined dense inputmode="decimal" :rules="[reglaPrecios]" />
      </div>
    </fieldset>

    <q-btn type="submit" unelevated no-caps color="primary" label="Aplicar filtros" icon="filter_alt" />
    <q-btn flat no-caps color="grey-8" label="Limpiar" @click="$emit('limpiar')" />
  </q-form>
</template>

<script setup>
import { hoyIso } from '@/utils/formato'

const modelo = defineModel({ type: Object, required: true })
defineProps({ destinos: { type: Array, default: () => [] } })
defineEmits(['aplicar', 'limpiar'])

const hoy = hoyIso()
const reglaFechas = () =>
  !modelo.value.desde || !modelo.value.hasta || modelo.value.hasta >= modelo.value.desde || 'Debe ser posterior a "Desde".'
const reglaPrecios = () =>
  !modelo.value.precioMin || !modelo.value.precioMax || Number(modelo.value.precioMax) >= Number(modelo.value.precioMin) ||
  'Debe ser mayor o igual al mínimo.'
</script>

<style scoped>
.grupo {
  border: 0;
  padding: 0;
  margin: 0;
}

.grupo legend {
  margin-bottom: 6px;
  padding: 0;
}
</style>
