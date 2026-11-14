<template>
  <q-page class="tg-contenedor q-py-lg">
    <EncabezadoPagina titulo="Panel de administración" subtitulo="Estado general de la plataforma." />

    <EstadoError v-if="error" :mensaje="error" @reintentar="cargar" />
    <template v-else>
      <q-banner v-if="r?.empresasPendientes" rounded class="bg-amber-1 q-mb-lg">
        <template #avatar><q-icon name="hourglass_top" color="brown-8" /></template>
        Hay {{ pluralizar(r.empresasPendientes, 'empresa pendiente', 'empresas pendientes') }} de verificación.
        <template #action><q-btn flat no-caps color="brown-9" label="Revisar ahora" to="/admin/empresas" /></template>
      </q-banner>

      <div class="indicadores">
        <template v-if="cargando"><q-skeleton v-for="n in 5" :key="n" height="96px" class="rounded-borders" /></template>
        <template v-else>
          <div v-for="k in indicadores" :key="k.etiqueta" class="indicador tg-tarjeta">
            <q-icon :name="k.icono" size="24px" color="primary" />
            <div class="indicador__valor">{{ k.valor }}</div>
            <div class="indicador__etiqueta">{{ k.etiqueta }}</div>
          </div>
        </template>
      </div>

      <div class="row q-col-gutter-lg q-mt-sm">
        <section class="col-12 col-md-7">
          <div class="tg-tarjeta q-pa-md">
            <q-skeleton v-if="cargando" height="240px" />
            <GraficoReservasMes v-else titulo="Reservas por mes (últimos 6 meses)" :datos="r.reservasPorMes" />
          </div>
        </section>
        <section class="col-12 col-md-5 column q-gutter-y-lg">
          <div class="tg-tarjeta q-pa-md">
            <h2 class="text-h6 q-mt-none q-mb-sm">Destinos más reservados</h2>
            <q-skeleton v-if="cargando" height="120px" />
            <p v-else-if="!r.topDestinos.length" class="tg-texto-suave">Aún no hay reservas.</p>
            <q-list v-else dense separator>
              <q-item v-for="(d, i) in r.topDestinos" :key="d.destino">
                <q-item-section avatar class="text-weight-bold tg-texto-suave">{{ i + 1 }}</q-item-section>
                <q-item-section>{{ d.destino }}</q-item-section>
                <q-item-section side>{{ pluralizar(d.reservas, 'reserva') }} · {{ pluralizar(d.personas, 'persona') }}</q-item-section>
              </q-item>
            </q-list>
          </div>
          <div class="tg-tarjeta q-pa-md">
            <h2 class="text-h6 q-mt-none q-mb-sm">Empresas destacadas</h2>
            <q-skeleton v-if="cargando" height="120px" />
            <q-list v-else dense separator>
              <q-item v-for="e in r.topEmpresas" :key="e.id">
                <q-item-section>{{ e.nombreComercial }}</q-item-section>
                <q-item-section side>
                  <ValoracionEstrellas v-if="e.valoracion" :valor="e.valoracion" :total="e.totalResenas" tamano="16px" />
                  <span class="text-caption">{{ pluralizar(e.reservas, 'reserva activa', 'reservas activas') }}</span>
                </q-item-section>
              </q-item>
            </q-list>
          </div>
        </section>
      </div>
    </template>
  </q-page>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import EncabezadoPagina from '@/components/EncabezadoPagina.vue'
import EstadoError from '@/components/EstadoError.vue'
import GraficoReservasMes from '@/components/GraficoReservasMes.vue'
import ValoracionEstrellas from '@/components/ValoracionEstrellas.vue'
import { adminApi } from '@/services/api'
import { mensajeError } from '@/utils/errores'
import { pluralizar } from '@/utils/formato'

const r = ref(null)
const cargando = ref(true)
const error = ref('')

const indicadores = computed(() => [
  { icono: 'backpack', valor: r.value.turistas, etiqueta: 'Viajeros registrados' },
  { icono: 'storefront', valor: r.value.empresas, etiqueta: `Empresas (${pluralizar(r.value.empresasPendientes, 'pendiente')})` },
  { icono: 'public', valor: r.value.actividadesPublicadas, etiqueta: 'Actividades publicadas vigentes' },
  { icono: 'confirmation_number', valor: r.value.reservasActivas, etiqueta: 'Reservas activas' },
  { icono: 'visibility_off', valor: r.value.resenasOcultas, etiqueta: 'Reseñas ocultas' },
])

async function cargar() {
  cargando.value = true
  error.value = ''
  try {
    r.value = await adminApi.resumen()
  } catch (e) {
    error.value = mensajeError(e)
  } finally {
    cargando.value = false
  }
}

onMounted(cargar)
</script>

<style scoped>
.indicadores {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 16px;
}

.indicador {
  padding: 16px;
}

.indicador__valor {
  font-size: 1.6rem;
  font-weight: 700;
  margin-top: 4px;
}

.indicador__etiqueta {
  color: var(--tg-texto-suave);
  font-size: 0.88rem;
}
</style>
