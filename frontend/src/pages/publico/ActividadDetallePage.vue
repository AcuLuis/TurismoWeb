<template>
  <q-page class="q-pb-xl" :class="{ 'con-barra-movil': $q.screen.lt.md && actividad?.reservable }">
    <!-- Cargando -->
    <div v-if="cargando" class="tg-contenedor q-py-lg">
      <q-skeleton height="320px" class="q-mb-lg rounded-borders" />
      <q-skeleton type="text" class="text-h4" width="60%" />
      <q-skeleton type="text" width="40%" />
    </div>

    <!-- No encontrada o error -->
    <div v-else-if="error" class="tg-contenedor q-py-xl">
      <EstadoVacio
        v-if="noEncontrada"
        icono="travel_explore"
        titulo="Esta actividad no está disponible"
        mensaje="Es posible que haya sido retirada o que el enlace no sea correcto."
      >
        <q-btn unelevated no-caps color="primary" label="Explorar otras actividades" to="/actividades" />
      </EstadoVacio>
      <EstadoError v-else :mensaje="error" @reintentar="cargar" />
    </div>

    <template v-else-if="actividad">
      <ActividadPortada :destino="actividad.destino" :imagen-url="actividad.imagenUrl" :alt="actividad.nombre" :alto="$q.screen.lt.sm ? '200px' : '320px'" />

      <div class="tg-contenedor">
        <q-banner v-if="actividad.estado !== 'Publicada'" class="bg-amber-1 q-mt-md" rounded>
          <template #avatar><q-icon name="visibility" color="brown-8" /></template>
          Vista previa ({{ actividad.estado.toLowerCase() }}): solo tu empresa y los administradores ven esta página.
        </q-banner>

        <q-breadcrumbs class="q-mt-md text-caption" active-color="grey-8">
          <q-breadcrumbs-el label="Actividades" to="/actividades" />
          <q-breadcrumbs-el :label="actividad.destino" :to="{ path: '/actividades', query: { destino: actividad.destino } }" />
          <q-breadcrumbs-el :label="actividad.nombre" />
        </q-breadcrumbs>

        <div class="detalle">
          <article class="detalle__principal">
            <h1 class="tg-titulo-pagina q-mt-sm">{{ actividad.nombre }}</h1>
            <div class="detalle__meta">
              <span><q-icon name="place" aria-hidden="true" /> {{ actividad.destino }}</span>
              <span><q-icon name="schedule" aria-hidden="true" /> {{ duracionTexto(actividad.duracionDias) }}</span>
              <ValoracionEstrellas v-if="actividad.valoracion" :valor="actividad.valoracion" :total="actividad.totalResenas" />
            </div>

            <div class="operador tg-tarjeta">
              <q-avatar color="primary" text-color="white" icon="storefront" size="44px" />
              <div class="col">
                <div class="tg-etiqueta">Operado por</div>
                <div class="text-weight-medium">{{ actividad.empresa.nombreComercial }} <q-icon name="verified" color="primary" size="16px"><q-tooltip>Empresa verificada por TurismoGo</q-tooltip></q-icon></div>
                <ValoracionEstrellas :valor="actividad.empresa.valoracion" :total="actividad.empresa.totalResenas" sufijo="en todas sus actividades" tamano="15px" class="text-caption" />
              </div>
            </div>

            <!-- En móvil el panel de reserva va arriba -->
            <div v-if="$q.screen.lt.md" id="panel-reserva" class="q-mt-md">
              <ReservaPanel :actividad="actividad" @reservada="cargar" />
            </div>

            <section class="q-mt-lg" aria-labelledby="titulo-descripcion">
              <h2 id="titulo-descripcion" class="seccion-titulo">Descripción</h2>
              <p class="texto-largo">{{ actividad.descripcion }}</p>
              <p v-if="actividad.empresa.descripcion" class="tg-texto-suave">{{ actividad.empresa.descripcion }}</p>
            </section>

            <section class="q-mt-lg" aria-labelledby="titulo-itinerario">
              <h2 id="titulo-itinerario" class="seccion-titulo">Itinerario</h2>
              <q-timeline color="primary" layout="dense">
                <q-timeline-entry
                  v-for="dia in actividad.itinerario"
                  :key="dia.dia"
                  :title="dia.titulo"
                  :subtitle="`Día ${dia.dia} · ${formatFecha(sumarDias(actividad.fechaInicio, dia.dia - 1), 'largo')}`"
                >
                  <p class="q-mb-none">{{ dia.descripcion }}</p>
                </q-timeline-entry>
              </q-timeline>
            </section>

            <section class="q-mt-md row q-col-gutter-md" aria-label="Qué incluye">
              <div v-if="incluye.length" class="col-12 col-sm-6">
                <h2 class="seccion-titulo">Incluye</h2>
                <ul class="lista-iconos">
                  <li v-for="i in incluye" :key="i"><q-icon name="check_circle" color="positive" aria-hidden="true" /> {{ i }}</li>
                </ul>
              </div>
              <div v-if="noIncluye.length" class="col-12 col-sm-6">
                <h2 class="seccion-titulo">No incluye</h2>
                <ul class="lista-iconos">
                  <li v-for="i in noIncluye" :key="i"><q-icon name="remove_circle_outline" color="grey-7" aria-hidden="true" /> {{ i }}</li>
                </ul>
              </div>
            </section>

            <section class="q-mt-md" aria-labelledby="titulo-encuentro">
              <h2 id="titulo-encuentro" class="seccion-titulo">Punto de encuentro</h2>
              <p><q-icon name="pin_drop" color="secondary" aria-hidden="true" /> {{ actividad.puntoEncuentro }}</p>
            </section>

            <ResenasActividad class="q-mt-lg" :actividad-id="actividad.id" />
          </article>

          <aside v-if="$q.screen.gt.sm" class="detalle__lateral" aria-label="Reserva">
            <ReservaPanel :actividad="actividad" @reservada="cargar" />
          </aside>
        </div>
      </div>

      <!-- Barra fija en móvil -->
      <div v-if="$q.screen.lt.md && actividad.reservable" class="barra-movil">
        <div>
          <div class="text-weight-bold text-primary">{{ formatMoneda(actividad.precio) }}</div>
          <div class="text-caption tg-texto-suave">por persona</div>
        </div>
        <q-btn unelevated no-caps color="secondary" label="Reservar" @click="irAlPanel" />
      </div>
    </template>
  </q-page>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import ActividadPortada from '@/components/ActividadPortada.vue'
import EstadoError from '@/components/EstadoError.vue'
import EstadoVacio from '@/components/EstadoVacio.vue'
import ReservaPanel from '@/components/ReservaPanel.vue'
import ResenasActividad from '@/components/ResenasActividad.vue'
import ValoracionEstrellas from '@/components/ValoracionEstrellas.vue'
import { catalogoApi } from '@/services/api'
import { estadoHttp, mensajeError } from '@/utils/errores'
import { duracionTexto, formatFecha, formatMoneda, sumarDias } from '@/utils/formato'

const route = useRoute()
const actividad = ref(null)
const cargando = ref(true)
const error = ref('')
const noEncontrada = ref(false)

/** Separa listas escritas por líneas, comas o punto y coma. */
const separar = (texto) =>
  (texto || '')
    .split(/\r?\n|;|,(?![^(]*\))/)
    .map((s) => s.trim().replace(/\.$/, ''))
    .filter(Boolean)
    .map((s) => s.charAt(0).toUpperCase() + s.slice(1))

const incluye = computed(() => separar(actividad.value?.incluye))
const noIncluye = computed(() => separar(actividad.value?.noIncluye))

async function cargar() {
  // Recarga silenciosa si ya hay datos (por ejemplo, tras reservar)
  if (!actividad.value || actividad.value.id !== Number(route.params.id)) cargando.value = true
  error.value = ''
  noEncontrada.value = false
  try {
    actividad.value = await catalogoApi.detalle(route.params.id)
    document.title = `${actividad.value.nombre} | TurismoGo`
  } catch (e) {
    noEncontrada.value = estadoHttp(e) === 404
    error.value = mensajeError(e)
  } finally {
    cargando.value = false
  }
}

function irAlPanel() {
  document.getElementById('panel-reserva')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

watch(() => route.params.id, (id) => id && cargar(), { immediate: true })
</script>

<style scoped lang="scss">
.detalle {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 360px;
  gap: 32px;
  align-items: start;

  @media (max-width: 1023px) {
    grid-template-columns: 1fr;
  }
}

.detalle__lateral {
  position: sticky;
  top: 84px;
  margin-top: 20px;
}

.detalle__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 18px;
  color: var(--tg-texto-suave);
  margin: 8px 0 16px;

  .q-icon {
    color: $primary;
  }
}

.operador {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 14px 16px;
}

.seccion-titulo,
:deep(.seccion-titulo) {
  font-size: 1.2rem;
  font-weight: 600;
  margin: 0 0 10px;
}

.texto-largo {
  white-space: pre-line;
  line-height: 1.65;
}

.lista-iconos {
  list-style: none;
  padding: 0;
  margin: 0;
  display: grid;
  gap: 6px;
}

.con-barra-movil {
  padding-bottom: 96px !important;
}

.barra-movil {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 1000;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 16px calc(10px + env(safe-area-inset-bottom));
  background: #fff;
  border-top: 1px solid var(--tg-borde);
  box-shadow: 0 -4px 16px rgba(15, 23, 42, 0.08);
}
</style>
