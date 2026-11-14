<template>
  <q-dialog :model-value="modelValue" persistent @update:model-value="$emit('update:modelValue', $event)">
    <q-card class="dialogo">
      <q-card-section>
        <h2 class="text-h6 q-my-none">Cuéntanos tu experiencia</h2>
        <p class="tg-texto-suave q-mb-none">{{ actividad?.nombre }}</p>
      </q-card-section>

      <q-form greedy @submit="enviar" @validation-error="enviado = true">
        <q-card-section class="q-pt-none q-gutter-y-md">
          <div>
            <div id="etiqueta-valoracion" class="tg-etiqueta q-mb-xs">Tu valoración</div>
            <q-rating
              v-model="valoracion"
              :max="5"
              size="34px"
              color="warning"
              icon="star_border"
              icon-selected="star"
              aria-labelledby="etiqueta-valoracion"
            />
            <div class="text-caption tg-texto-suave">{{ textoValoracion }}</div>
            <div v-if="enviado && !valoracion" class="text-caption text-negative">Selecciona de 1 a 5 estrellas.</div>
          </div>
          <q-input
            v-model="comentario"
            type="textarea"
            label="Comentario"
            outlined
            autogrow
            counter
            maxlength="1000"
            :rules="[requerido('Escribe un comentario.'), minLongitud(10)]"
            :error="!!errorServidor.comentario"
            :error-message="errorServidor.comentario"
            hint="¿Qué fue lo mejor? ¿Qué podría mejorar? Tu nombre se mostrará abreviado."
          />
        </q-card-section>

        <q-card-actions align="right" class="q-px-md q-pb-md">
          <q-btn v-close-popup flat no-caps color="grey-8" label="Cancelar" :disable="guardando" />
          <q-btn type="submit" unelevated no-caps color="primary" label="Publicar reseña" :loading="guardando" />
        </q-card-actions>
      </q-form>
    </q-card>
  </q-dialog>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { turistaApi } from '@/services/api'
import { useAvisos } from '@/composables/useAvisos'
import { erroresDeCampo } from '@/utils/errores'
import { minLongitud, requerido } from '@/utils/reglas'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  actividad: { type: Object, default: null },
})
const emit = defineEmits(['update:modelValue', 'publicada'])
const avisos = useAvisos()

const valoracion = ref(0)
const comentario = ref('')
const guardando = ref(false)
const enviado = ref(false)
const errorServidor = ref({})

const textoValoracion = computed(
  () => ['Sin calificar', 'Mala', 'Regular', 'Buena', 'Muy buena', 'Excelente'][valoracion.value] ?? '',
)

watch(
  () => props.modelValue,
  (abierto) => {
    if (abierto) {
      valoracion.value = 0
      comentario.value = ''
      enviado.value = false
      errorServidor.value = {}
    }
  },
)

async function enviar() {
  enviado.value = true
  if (!valoracion.value) return
  guardando.value = true
  errorServidor.value = {}
  try {
    await turistaApi.resenar(props.actividad.id, valoracion.value, comentario.value.trim())
    avisos.exito('¡Gracias! Tu reseña ya está publicada.')
    emit('publicada')
    emit('update:modelValue', false)
  } catch (error) {
    errorServidor.value = erroresDeCampo(error)
    if (!Object.keys(errorServidor.value).length) avisos.error(error)
  } finally {
    guardando.value = false
  }
}
</script>

<style scoped>
.dialogo {
  width: 520px;
  max-width: 92vw;
  border-radius: 16px;
}
</style>
