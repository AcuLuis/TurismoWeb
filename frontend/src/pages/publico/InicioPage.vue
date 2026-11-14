<template>
  <q-page>
    <!-- Portada con buscador -->
    <section class="hero">
      <svg class="hero__montes" viewBox="0 0 1200 240" preserveAspectRatio="none" aria-hidden="true">
        <path d="M0 240 L160 90 L260 160 L420 30 L560 150 L700 70 L860 170 L1000 60 L1200 180 L1200 240 Z" fill="rgba(255,255,255,0.08)" />
        <path d="M0 240 L200 160 L360 210 L520 140 L720 205 L900 150 L1200 200 L1200 240 Z" fill="rgba(255,255,255,0.12)" />
      </svg>
      <div class="tg-contenedor hero__contenido">
        <h1 class="hero__titulo">Descubre el Perú con operadores verificados</h1>
        <p class="hero__texto">
          Trekking, cultura, gastronomía y naturaleza. Reserva en minutos con cupos y precios actualizados.
        </p>

        <q-form class="buscador" role="search" @submit="buscar">
          <q-select
            v-model="destino"
            :options="destinos"
            label="¿A dónde quieres ir?"
            outlined
            bg-color="white"
            clearable
            emit-value
            class="buscador__campo"
            :loading="cargandoDestinos"
          >
            <template #prepend><q-icon name="place" /></template>
          </q-select>
          <q-input v-model="desde" type="date" label="Desde" stack-label outlined bg-color="white" class="buscador__fecha" :min="hoy" />
          <q-btn type="submit" color="secondary" unelevated no-caps icon="search" label="Buscar" class="buscador__boton" />
        </q-form>
      </div>
    </section>

    <!-- Próximas salidas -->
    <section class="tg-contenedor q-py-xl" aria-labelledby="titulo-proximas">
      <div class="row items-end justify-between q-mb-md q-col-gutter-sm">
        <div>
          <h2 id="titulo-proximas" class="seccion__titulo">Próximas salidas</h2>
          <p class="tg-subtitulo">Actividades con cupos disponibles en las próximas semanas.</p>
        </div>
        <q-btn flat no-caps color="primary" icon-right="arrow_forward" label="Ver todas" to="/actividades" />
      </div>

      <EstadoError v-if="error" :mensaje="error" @reintentar="cargar" />
      <div v-else class="grilla">
        <template v-if="cargando">
          <ActividadCardSkeleton v-for="n in 6" :key="n" />
        </template>
        <ActividadCard v-for="a in actividades" v-else :key="a.id" :actividad="a" />
      </div>
      <EstadoVacio
        v-if="!cargando && !error && actividades.length === 0"
        titulo="Pronto tendremos nuevas salidas"
        mensaje="Vuelve en unos días para descubrir nuevas actividades."
      />
    </section>

    <!-- Beneficios -->
    <section class="beneficios" aria-labelledby="titulo-beneficios">
      <div class="tg-contenedor q-py-xl">
        <h2 id="titulo-beneficios" class="seccion__titulo text-center q-mb-lg">¿Por qué reservar con TurismoGo?</h2>
        <div class="row q-col-gutter-lg">
          <div v-for="b in beneficios" :key="b.titulo" class="col-12 col-sm-4">
            <div class="beneficio">
              <q-icon :name="b.icono" size="32px" color="primary" aria-hidden="true" />
              <h3 class="beneficio__titulo">{{ b.titulo }}</h3>
              <p class="tg-texto-suave q-mb-none">{{ b.texto }}</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Llamado a empresas -->
    <section v-if="!auth.autenticado" class="tg-contenedor q-py-xl">
      <div class="cta">
        <div>
          <h2 class="cta__titulo">¿Tienes una empresa de turismo?</h2>
          <p class="q-mb-none">Publica tus actividades, gestiona cupos y reservas, y exporta tus listas de pasajeros.</p>
        </div>
        <q-btn color="white" text-color="primary" unelevated no-caps label="Registrar mi empresa" to="/registro?tipo=empresa" />
      </div>
    </section>
  </q-page>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import ActividadCard from '@/components/ActividadCard.vue'
import ActividadCardSkeleton from '@/components/ActividadCardSkeleton.vue'
import EstadoError from '@/components/EstadoError.vue'
import EstadoVacio from '@/components/EstadoVacio.vue'
import { catalogoApi } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { mensajeError } from '@/utils/errores'
import { hoyIso } from '@/utils/formato'

const auth = useAuthStore()
const router = useRouter()
const hoy = hoyIso()

const actividades = ref([])
const cargando = ref(true)
const error = ref('')
const destinos = ref([])
const cargandoDestinos = ref(true)
const destino = ref(null)
const desde = ref('')

const beneficios = [
  { icono: 'verified', titulo: 'Operadores verificados', texto: 'Revisamos el RUC y los datos de cada empresa antes de que publique.' },
  { icono: 'event_available', titulo: 'Cupos en tiempo real', texto: 'Ves exactamente cuántos lugares quedan y tu reserva los aparta al instante.' },
  { icono: 'reviews', titulo: 'Reseñas reales', texto: 'Solo opinan viajeros que completaron la actividad con una reserva confirmada.' },
]

async function cargar() {
  cargando.value = true
  error.value = ''
  try {
    actividades.value = (await catalogoApi.buscar({ tamano: 6 })).items
  } catch (e) {
    error.value = mensajeError(e)
  } finally {
    cargando.value = false
  }
}

async function cargarDestinos() {
  try {
    destinos.value = await catalogoApi.destinos()
  } catch {
    destinos.value = []
  } finally {
    cargandoDestinos.value = false
  }
}

function buscar() {
  router.push({ path: '/actividades', query: { ...(destino.value && { destino: destino.value }), ...(desde.value && { desde: desde.value }) } })
}

onMounted(() => {
  cargar()
  cargarDestinos()
})
</script>

<style scoped lang="scss">
.hero {
  position: relative;
  background: linear-gradient(150deg, #0f766e 0%, #134e4a 55%, #0f172a 100%);
  color: #fff;
  overflow: hidden;
}

.hero__montes {
  position: absolute;
  left: 0;
  right: 0;
  bottom: 0;
  width: 100%;
  height: 200px;
}

.hero__contenido {
  position: relative;
  padding-top: 72px;
  padding-bottom: 88px;

  @media (max-width: 599px) {
    padding-top: 40px;
    padding-bottom: 56px;
  }
}

.hero__titulo {
  font-size: clamp(1.9rem, 4.5vw, 3rem);
  line-height: 1.15;
  font-weight: 700;
  margin: 0;
  max-width: 720px;
  letter-spacing: -0.02em;
}

.hero__texto {
  font-size: 1.15rem;
  max-width: 580px;
  margin: 14px 0 28px;
  color: #ccfbf1;
}

.buscador {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  max-width: 820px;
  background: rgba(255, 255, 255, 0.12);
  padding: 10px;
  border-radius: 16px;
}

.buscador__campo {
  flex: 1 1 260px;
}

.buscador__fecha {
  flex: 0 1 190px;

  @media (max-width: 599px) {
    flex: 1 1 100%;
  }
}

.buscador__boton {
  flex: 0 0 auto;
  min-height: 56px;
  padding: 0 24px;

  @media (max-width: 599px) {
    flex: 1 1 100%;
  }
}

.seccion__titulo {
  font-size: 1.6rem;
  font-weight: 600;
  margin: 0;
}

.grilla {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 20px;
}

.beneficios {
  background: #fff;
  border-top: 1px solid var(--tg-borde);
  border-bottom: 1px solid var(--tg-borde);
}

.beneficio {
  text-align: center;
  padding: 8px 12px;
}

.beneficio__titulo {
  font-size: 1.1rem;
  font-weight: 600;
  margin: 10px 0 6px;
}

.cta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 28px 32px;
  border-radius: 20px;
  background: linear-gradient(120deg, #c2410c, #9a3412);
  color: #fff;
}

.cta__titulo {
  font-size: 1.4rem;
  font-weight: 600;
  margin: 0 0 6px;
}
</style>
