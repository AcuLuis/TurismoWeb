<template>
  <div class="panel tg-tarjeta">
    <div class="panel__precio">
      <span class="text-h5 text-weight-bold text-primary">{{ formatMoneda(actividad.precio) }}</span>
      <span class="tg-texto-suave"> por persona</span>
    </div>

    <ul class="panel__datos">
      <li><q-icon name="event" aria-hidden="true" /> {{ formatRangoFechas(actividad.fechaInicio, actividad.fechaFin) }}</li>
      <li><q-icon name="schedule" aria-hidden="true" /> {{ duracionTexto(actividad.duracionDias) }}</li>
      <li>
        <q-icon name="groups" aria-hidden="true" />
        <span v-if="actividad.cuposDisponibles > 0">{{ pluralizar(actividad.cuposDisponibles, 'cupo disponible', 'cupos disponibles') }}</span>
        <span v-else class="text-negative">Sin cupos disponibles</span>
      </li>
    </ul>

    <!-- No se puede reservar -->
    <q-banner v-if="motivoNoReservable" rounded class="bg-grey-2 q-mt-md">
      <template #avatar><q-icon :name="motivoNoReservable.icono" color="grey-8" /></template>
      {{ motivoNoReservable.texto }}
    </q-banner>

    <!-- Invitado -->
    <template v-else-if="!auth.autenticado">
      <q-btn
        unelevated
        no-caps
        color="secondary"
        size="lg"
        class="full-width q-mt-md"
        label="Ingresa para reservar"
        :to="{ path: '/ingresar', query: { redirect: $route.fullPath } }"
      />
      <p class="text-caption text-center tg-texto-suave q-mt-sm q-mb-none">
        ¿Nuevo en TurismoGo?
        <router-link :to="{ path: '/registro', query: { redirect: $route.fullPath } }" class="text-primary">Crea tu cuenta gratis</router-link>
      </p>
    </template>

    <!-- Otros roles -->
    <q-banner v-else-if="!auth.esTurista" rounded class="bg-teal-1 q-mt-md">
      <template #avatar><q-icon name="info" color="primary" /></template>
      Las reservas se hacen con una cuenta de viajero.
    </q-banner>

    <!-- Formulario de reserva -->
    <q-form v-else class="q-mt-md" @submit="confirmar">
      <div class="row items-center justify-between">
        <label id="etiqueta-personas" class="text-body2 text-weight-medium">Personas</label>
        <div class="contador" role="group" aria-labelledby="etiqueta-personas">
          <q-btn round dense flat icon="remove" aria-label="Quitar una persona" :disable="personas <= 1" @click="personas--" />
          <span class="contador__valor" aria-live="polite">{{ personas }}</span>
          <q-btn round dense flat icon="add" aria-label="Agregar una persona" :disable="personas >= maxPersonas" @click="personas++" />
        </div>
      </div>
      <div v-if="personas >= maxPersonas" class="text-caption tg-texto-suave text-right">Máximo para esta reserva</div>

      <q-expansion-item dense dense-toggle label="Agregar una nota para la empresa" class="q-mt-sm nota" header-class="q-px-none text-primary">
        <q-input v-model="notas" type="textarea" outlined autogrow counter maxlength="500" placeholder="Ej.: alergias, restricciones alimentarias, hotel de recojo…" class="q-mt-xs" />
      </q-expansion-item>

      <q-separator class="q-my-md" />
      <div class="row justify-between items-center text-body1">
        <span>Total ({{ pluralizar(personas, 'persona') }})</span>
        <strong class="text-h6">{{ formatMoneda(total) }}</strong>
      </div>

      <q-btn type="submit" unelevated no-caps color="secondary" size="lg" class="full-width q-mt-md" label="Reservar ahora" :loading="reservando" />
      <p class="text-caption tg-texto-suave q-mt-sm q-mb-none">
        <q-icon name="verified_user" size="14px" aria-hidden="true" />
        Tus cupos quedan apartados al instante. Puedes cancelar sin costo hasta {{ diasCancelacion }} días antes del inicio.
      </p>
    </q-form>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { turistaApi } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useAvisos } from '@/composables/useAvisos'
import { duracionTexto, formatFecha, formatMoneda, formatRangoFechas, pluralizar, sumarDias } from '@/utils/formato'
import { estadoHttp, mensajeError } from '@/utils/errores'
import { useQuasar } from 'quasar'

const props = defineProps({
  actividad: { type: Object, required: true },
})
const emit = defineEmits(['reservada'])

const MAX_POR_RESERVA = 20
const diasCancelacion = 2 // Igual a ReglasNegocio:DiasMinimosCancelacion del backend (el servidor siempre valida)
const auth = useAuthStore()
const avisos = useAvisos()
const router = useRouter()
const $q = useQuasar()

const personas = ref(1)
const notas = ref('')
const reservando = ref(false)

const maxPersonas = computed(() => Math.max(1, Math.min(MAX_POR_RESERVA, props.actividad.cuposDisponibles)))
const total = computed(() => props.actividad.precio * personas.value)

const motivoNoReservable = computed(() => {
  const a = props.actividad
  if (a.estado === 'Cancelada') return { icono: 'event_busy', texto: 'Esta actividad fue cancelada por la empresa.' }
  if (a.finalizada) return { icono: 'history', texto: 'Esta salida ya se realizó.' }
  if (a.estado !== 'Publicada') return { icono: 'visibility', texto: 'Vista previa: la actividad aún no está publicada.' }
  if (a.cuposDisponibles === 0) return { icono: 'event_busy', texto: 'No quedan cupos para esta salida.' }
  if (!a.reservable) return { icono: 'event_busy', texto: 'Esta actividad ya no admite reservas.' }
  return null
})

async function confirmar() {
  const ok = await avisos.confirmar({
    titulo: 'Confirma tu reserva',
    mensaje: `${props.actividad.nombre} · ${formatRangoFechas(props.actividad.fechaInicio, props.actividad.fechaFin)} · ${pluralizar(personas.value, 'persona')} · Total ${formatMoneda(total.value)}`,
    textoAceptar: 'Confirmar reserva',
  })
  if (!ok) return

  reservando.value = true
  try {
    const reserva = await turistaApi.reservar(props.actividad.id, personas.value, notas.value.trim())
    emit('reservada')
    $q.dialog({
      title: '¡Reserva registrada!',
      message:
        `Tu código es <strong>${reserva.codigo}</strong>. La empresa confirmará tu reserva pronto; ` +
        `puedes seguir su estado en "Mis reservas". Cancelación sin costo hasta el ${formatFecha(sumarDias(props.actividad.fechaInicio, -diasCancelacion))}.`,
      html: true,
      ok: { label: 'Ver mis reservas', color: 'primary', unelevated: true, noCaps: true },
      cancel: { label: 'Seguir explorando', flat: true, noCaps: true, color: 'grey-8' },
    }).onOk(() => router.push('/mis-reservas'))
    personas.value = 1
    notas.value = ''
  } catch (error) {
    if (estadoHttp(error) === 409) {
      $q.dialog({
        title: 'Ya tienes una reserva',
        message: mensajeError(error),
        ok: { label: 'Ir a mis reservas', color: 'primary', unelevated: true, noCaps: true },
        cancel: { label: 'Cerrar', flat: true, noCaps: true, color: 'grey-8' },
      }).onOk(() => router.push('/mis-reservas'))
    } else {
      avisos.error(error)
      emit('reservada') // refresca los cupos si cambiaron
    }
  } finally {
    reservando.value = false
  }
}
</script>

<style scoped lang="scss">
.panel {
  padding: 20px;
}

.panel__datos {
  list-style: none;
  padding: 0;
  margin: 12px 0 0;
  display: grid;
  gap: 6px;

  .q-icon {
    color: $primary;
    margin-right: 6px;
  }
}

.contador {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  border: 1px solid var(--tg-borde);
  border-radius: 999px;
  padding: 2px;
}

.contador__valor {
  min-width: 28px;
  text-align: center;
  font-weight: 600;
  font-size: 1.05rem;
}

.nota :deep(.q-item) {
  min-height: 32px;
}
</style>
