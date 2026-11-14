<template>
  <q-page class="tg-contenedor tg-contenedor--estrecho q-py-lg">
    <EncabezadoPagina titulo="Mis reservas" subtitulo="Consulta el estado de tus reservas, cancélalas o comparte tu experiencia." />

    <q-tabs
      :model-value="pestana"
      align="left"
      no-caps
      active-color="primary"
      indicator-color="primary"
      class="text-grey-8 q-mb-md"
      narrow-indicator
      @update:model-value="cambiarPestana"
    >
      <q-tab name="proximas" label="Próximas" icon="upcoming" />
      <q-tab name="pasadas" label="Realizadas" icon="history" />
      <q-tab name="canceladas" label="Canceladas" icon="event_busy" />
    </q-tabs>

    <EstadoError v-if="error" :mensaje="error" @reintentar="cargar" />

    <div v-else-if="cargando" class="q-gutter-y-md">
      <q-skeleton v-for="n in 3" :key="n" height="150px" class="rounded-borders" />
    </div>

    <EstadoVacio v-else-if="resultado.items.length === 0" v-bind="vacio">
      <q-btn v-if="pestana !== 'canceladas'" unelevated no-caps color="primary" label="Explorar actividades" to="/actividades" />
    </EstadoVacio>

    <ul v-else class="lista">
      <li v-for="r in resultado.items" :key="r.id" class="reserva tg-tarjeta">
        <ActividadPortada class="reserva__portada" :destino="r.actividad.destino" :imagen-url="r.actividad.imagenUrl" :alto="$q.screen.lt.sm ? '110px' : '100%'" />
        <div class="reserva__cuerpo">
          <div class="row items-start justify-between q-gutter-sm no-wrap">
            <div>
              <router-link :to="`/actividades/${r.actividad.id}`" class="reserva__titulo">{{ r.actividad.nombre }}</router-link>
              <div class="text-body2 tg-texto-suave">
                <q-icon name="event" aria-hidden="true" /> {{ formatRangoFechas(r.actividad.fechaInicio, r.actividad.fechaFin) }}
                · {{ r.actividad.destino }}
              </div>
            </div>
            <EstadoReservaChip :estado="r.estado" />
          </div>

          <dl class="reserva__datos">
            <div>
              <dt>Código</dt>
              <dd>
                <span class="text-weight-medium">{{ r.codigo }}</span>
                <q-btn flat round dense size="sm" icon="content_copy" :aria-label="`Copiar código ${r.codigo}`" @click="copiar(r.codigo)" />
              </dd>
            </div>
            <div><dt>Personas</dt><dd>{{ r.cantidadPersonas }}</dd></div>
            <div><dt>Total</dt><dd class="text-weight-medium">{{ formatMoneda(r.total) }}</dd></div>
            <div><dt>Operador</dt><dd>{{ r.actividad.empresa }}</dd></div>
          </dl>

          <p v-if="r.estado === 'Pendiente'" class="aviso aviso--pendiente">
            <q-icon name="schedule" aria-hidden="true" /> La empresa confirmará tu reserva pronto.
          </p>
          <p v-if="['Pendiente', 'Confirmada'].includes(r.estado)" class="aviso">
            <q-icon name="pin_drop" aria-hidden="true" /> {{ r.actividad.puntoEncuentro }}
            <template v-if="r.actividad.empresaTelefono"> · Contacto: <a :href="`tel:${r.actividad.empresaTelefono}`">{{ r.actividad.empresaTelefono }}</a></template>
          </p>
          <p v-if="r.motivoEstado && ['Cancelada', 'Rechazada'].includes(r.estado)" class="aviso aviso--cancelada">
            <q-icon name="info" aria-hidden="true" /> {{ r.motivoEstado }}
          </p>

          <div class="reserva__acciones">
            <q-btn v-if="r.puedeResenar" unelevated no-caps color="primary" icon="rate_review" label="Escribir reseña" @click="abrirResena(r)" />
            <span v-else-if="r.resenada" class="text-caption text-positive"><q-icon name="check" /> Ya dejaste tu reseña</span>
            <q-btn v-if="r.puedeCancelar" flat no-caps color="negative" label="Cancelar reserva" @click="cancelar(r)" />
            <span v-else-if="['Pendiente', 'Confirmada'].includes(r.estado) && pestana === 'proximas'" class="text-caption tg-texto-suave">
              El plazo para cancelar venció el {{ formatFecha(r.fechaLimiteCancelacion) }}.
            </span>
          </div>
        </div>
      </li>
    </ul>

    <div v-if="!cargando && resultado.totalPaginas > 1" class="row justify-center q-mt-lg">
      <q-pagination :model-value="resultado.pagina" :max="resultado.totalPaginas" direction-links color="primary" @update:model-value="cargar" />
    </div>

    <ResenaDialog v-model="dialogoResena" :actividad="actividadResena" @publicada="cargar(resultado.pagina)" />
  </q-page>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { copyToClipboard } from 'quasar'
import ActividadPortada from '@/components/ActividadPortada.vue'
import EncabezadoPagina from '@/components/EncabezadoPagina.vue'
import EstadoError from '@/components/EstadoError.vue'
import EstadoReservaChip from '@/components/EstadoReservaChip.vue'
import EstadoVacio from '@/components/EstadoVacio.vue'
import ResenaDialog from '@/components/ResenaDialog.vue'
import { useAvisos } from '@/composables/useAvisos'
import { turistaApi } from '@/services/api'
import { mensajeError } from '@/utils/errores'
import { formatFecha, formatMoneda, formatRangoFechas } from '@/utils/formato'

const route = useRoute()
const router = useRouter()
const avisos = useAvisos()

const pestanas = ['proximas', 'pasadas', 'canceladas']
const pestana = computed(() => (pestanas.includes(route.query.ver) ? route.query.ver : 'proximas'))
const resultado = ref({ items: [], pagina: 1, totalPaginas: 0 })
const cargando = ref(true)
const error = ref('')
const dialogoResena = ref(false)
const actividadResena = ref(null)

const vacio = computed(
  () =>
    ({
      proximas: { icono: 'luggage', titulo: 'No tienes reservas próximas', mensaje: 'Cuando reserves una actividad aparecerá aquí.' },
      pasadas: { icono: 'history', titulo: 'Aún no tienes actividades realizadas', mensaje: 'Después de tu viaje podrás dejar una reseña desde aquí.' },
      canceladas: { icono: 'event_available', titulo: 'No tienes reservas canceladas', mensaje: '' },
    })[pestana.value],
)

function cambiarPestana(valor) {
  router.replace({ query: valor === 'proximas' ? {} : { ver: valor } })
}

async function cargar(pagina = 1) {
  cargando.value = true
  error.value = ''
  try {
    resultado.value = await turistaApi.misReservas(pestana.value, pagina)
  } catch (e) {
    error.value = mensajeError(e)
  } finally {
    cargando.value = false
  }
}

async function cancelar(r) {
  const ok = await avisos.confirmar({
    titulo: 'Cancelar reserva',
    mensaje: `¿Seguro que deseas cancelar tu reserva ${r.codigo} de "${r.actividad.nombre}"? Se liberarán tus ${r.cantidadPersonas} cupo(s).`,
    textoAceptar: 'Sí, cancelar',
    textoCancelar: 'Mantener reserva',
    peligro: true,
  })
  if (!ok) return
  try {
    await turistaApi.cancelar(r.id)
    avisos.exito('Tu reserva fue cancelada.')
    cargar(resultado.value.pagina)
  } catch (e) {
    avisos.error(e)
  }
}

function abrirResena(r) {
  actividadResena.value = r.actividad
  dialogoResena.value = true
}

async function copiar(codigo) {
  try {
    await copyToClipboard(codigo)
    avisos.info(`Código ${codigo} copiado.`)
  } catch {
    avisos.info(`Tu código es ${codigo}.`)
  }
}

watch(pestana, () => cargar(1), { immediate: true })
</script>

<style scoped lang="scss">
.lista {
  list-style: none;
  padding: 0;
  margin: 0;
  display: grid;
  gap: 16px;
}

.reserva {
  display: grid;
  grid-template-columns: 170px 1fr;
  overflow: hidden;

  @media (max-width: 599px) {
    grid-template-columns: 1fr;
  }
}

.reserva__portada {
  min-height: 100%;
}

.reserva__cuerpo {
  padding: 16px;
  min-width: 0;
}

.reserva__titulo {
  font-weight: 600;
  font-size: 1.08rem;
  color: inherit;
  text-decoration: none;

  &:hover {
    color: $primary;
    text-decoration: underline;
  }
}

.reserva__datos {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
  gap: 8px 16px;
  margin: 12px 0 8px;

  dt {
    font-size: 0.75rem;
    color: var(--tg-texto-suave);
  }

  dd {
    margin: 0;
    display: flex;
    align-items: center;
  }
}

.aviso {
  margin: 6px 0 0;
  font-size: 0.9rem;
  color: var(--tg-texto-suave);

  &--pendiente {
    color: #92400e;
  }

  &--cancelada {
    color: #991b1b;
  }
}

.reserva__acciones {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-top: 12px;
}
</style>
