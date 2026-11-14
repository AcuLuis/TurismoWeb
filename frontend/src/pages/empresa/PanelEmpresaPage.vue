<template>
  <q-page class="tg-contenedor q-py-lg">
    <EncabezadoPagina :titulo="`Hola, ${auth.nombreCorto}`" :subtitulo="`Resumen de ${auth.empresa?.nombreComercial ?? 'tu empresa'}`">
      <template #acciones>
        <q-btn unelevated no-caps color="primary" icon="add" label="Nueva actividad" to="/empresa/actividades/nueva" />
      </template>
    </EncabezadoPagina>

    <EstadoError v-if="error" :mensaje="error" @reintentar="cargar" />

    <template v-else>
      <EstadoEmpresaBanner :estado="panel?.estado" :motivo="auth.empresa?.motivoEstado" />

      <!-- Indicadores -->
      <div class="indicadores">
        <template v-if="cargando">
          <q-skeleton v-for="n in 4" :key="n" height="104px" class="rounded-borders" />
        </template>
        <template v-else>
          <router-link to="/empresa/reservas?estado=Pendiente" class="indicador tg-tarjeta" :class="{ 'indicador--alerta': panel.reservasPendientes > 0 }">
            <q-icon name="pending_actions" size="26px" />
            <div class="indicador__valor">{{ panel.reservasPendientes }}</div>
            <div class="indicador__etiqueta">Reservas por confirmar</div>
          </router-link>
          <div class="indicador tg-tarjeta">
            <q-icon name="confirmation_number" size="26px" />
            <div class="indicador__valor">{{ panel.reservasMes }}</div>
            <div class="indicador__etiqueta">Reservas este mes · {{ pluralizar(panel.personasMes, 'persona') }}</div>
          </div>
          <div class="indicador tg-tarjeta">
            <q-icon name="payments" size="26px" />
            <div class="indicador__valor">{{ formatMoneda(panel.ingresosMes) }}</div>
            <div class="indicador__etiqueta">Ingresos confirmados del mes</div>
          </div>
          <div class="indicador tg-tarjeta">
            <q-icon name="star" size="26px" />
            <div class="indicador__valor">{{ panel.valoracion ? panel.valoracion.toFixed(1) : '—' }}</div>
            <div class="indicador__etiqueta">{{ panel.totalResenas ? `Valoración (${pluralizar(panel.totalResenas, 'reseña')})` : 'Aún sin reseñas' }}</div>
          </div>
        </template>
      </div>

      <div class="row q-col-gutter-lg q-mt-sm">
        <!-- Próximas salidas -->
        <section class="col-12 col-md-8" aria-labelledby="titulo-salidas">
          <div class="tg-tarjeta q-pa-md">
            <div class="row items-center justify-between q-mb-sm">
              <h2 id="titulo-salidas" class="text-h6 q-my-none">Próximas salidas</h2>
              <q-btn flat dense no-caps color="primary" label="Ver todas" to="/empresa/actividades" />
            </div>
            <q-skeleton v-if="cargando" height="160px" />
            <EstadoVacio
              v-else-if="panel.proximasSalidas.length === 0"
              icono="event_note"
              titulo="No tienes salidas publicadas"
              mensaje="Crea una actividad y publícala para empezar a recibir reservas."
            >
              <q-btn unelevated no-caps color="primary" label="Crear actividad" to="/empresa/actividades/nueva" />
            </EstadoVacio>
            <q-list v-else separator>
              <q-item v-for="s in panel.proximasSalidas" :key="s.actividadId" clickable :to="`/empresa/reservas?actividadId=${s.actividadId}`">
                <q-item-section>
                  <q-item-label class="text-weight-medium">{{ s.nombre }}</q-item-label>
                  <q-item-label caption>{{ formatFecha(s.fechaInicio, 'largo') }} · {{ textoFaltan(s.fechaInicio) }}</q-item-label>
                </q-item-section>
                <q-item-section side class="gt-xs" style="width: 140px">
                  <OcupacionBarra :reservados="s.cuposReservados" :capacidad="s.capacidad" />
                </q-item-section>
                <q-item-section side>
                  <q-badge v-if="s.reservasPendientes" color="secondary" :label="`${s.reservasPendientes} por confirmar`" />
                </q-item-section>
              </q-item>
            </q-list>
          </div>
        </section>

        <!-- Estado del catálogo -->
        <aside class="col-12 col-md-4" aria-label="Tu catálogo">
          <div class="tg-tarjeta q-pa-md">
            <h2 class="text-h6 q-mt-none q-mb-sm">Tu catálogo</h2>
            <q-skeleton v-if="cargando" height="80px" />
            <q-list v-else dense>
              <q-item clickable to="/empresa/actividades?estado=Publicada">
                <q-item-section avatar><q-icon name="public" color="positive" /></q-item-section>
                <q-item-section>Publicadas vigentes</q-item-section>
                <q-item-section side class="text-weight-bold">{{ panel.actividadesPublicadas }}</q-item-section>
              </q-item>
              <q-item clickable to="/empresa/actividades?estado=Borrador">
                <q-item-section avatar><q-icon name="edit_note" color="grey-7" /></q-item-section>
                <q-item-section>Borradores</q-item-section>
                <q-item-section side class="text-weight-bold">{{ panel.actividadesBorrador }}</q-item-section>
              </q-item>
            </q-list>
            <q-separator class="q-my-md" />
            <p class="text-caption tg-texto-suave q-mb-none">
              Consejo: confirma las reservas pendientes lo antes posible. Los viajeros valoran una respuesta rápida.
            </p>
          </div>
        </aside>
      </div>
    </template>
  </q-page>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import EncabezadoPagina from '@/components/EncabezadoPagina.vue'
import EstadoEmpresaBanner from '@/components/EstadoEmpresaBanner.vue'
import EstadoError from '@/components/EstadoError.vue'
import EstadoVacio from '@/components/EstadoVacio.vue'
import OcupacionBarra from '@/components/OcupacionBarra.vue'
import { empresaApi } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { mensajeError } from '@/utils/errores'
import { diasHasta, formatFecha, formatMoneda, pluralizar } from '@/utils/formato'

const auth = useAuthStore()
const panel = ref(null)
const cargando = ref(true)
const error = ref('')

const textoFaltan = (fecha) => {
  const d = diasHasta(fecha)
  return d === 0 ? 'hoy' : d === 1 ? 'mañana' : `en ${d} días`
}

async function cargar() {
  cargando.value = true
  error.value = ''
  try {
    panel.value = await empresaApi.panel()
  } catch (e) {
    error.value = mensajeError(e)
  } finally {
    cargando.value = false
  }
}

onMounted(cargar)
</script>

<style scoped lang="scss">
.indicadores {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));
  gap: 16px;
}

.indicador {
  padding: 16px;
  color: inherit;
  text-decoration: none;

  .q-icon {
    color: $primary;
  }

  &--alerta {
    border-color: #fdba74;
    background: #fff7ed;

    .q-icon {
      color: $secondary;
    }
  }
}

.indicador__valor {
  font-size: 1.6rem;
  font-weight: 700;
  margin-top: 6px;
}

.indicador__etiqueta {
  color: var(--tg-texto-suave);
  font-size: 0.9rem;
}
</style>
