<template>
  <q-page class="tg-contenedor q-py-lg">
    <EncabezadoPagina
      :titulo="esNueva ? 'Nueva actividad' : 'Editar actividad'"
      :subtitulo="esNueva ? 'Se guardará como borrador; podrás publicarla cuando esté lista.' : ''"
      :migas="[{ label: 'Mis actividades', to: '/empresa/actividades' }, { label: esNueva ? 'Nueva' : 'Editar' }]"
    >
      <template v-if="!esNueva && original" #acciones>
        <EstadoActividadChip :estado="original.estado" :finalizada="original.finalizada" />
      </template>
    </EncabezadoPagina>

    <div v-if="cargando" class="q-gutter-y-md">
      <q-skeleton v-for="n in 3" :key="n" height="160px" class="rounded-borders" />
    </div>
    <EstadoError v-else-if="errorCarga" :mensaje="errorCarga" @reintentar="cargar" />

    <template v-else>
      <q-banner v-if="soloLectura" rounded class="bg-grey-2 q-mb-lg">
        <template #avatar><q-icon name="lock" /></template>
        Esta actividad {{ original.estado === 'Cancelada' ? 'fue cancelada' : 'ya comenzó o finalizó' }} y no se puede editar.
        Puedes duplicarla para crear una nueva salida.
        <template #action>
          <q-btn flat no-caps color="primary" label="Duplicar" @click="duplicar" />
        </template>
      </q-banner>
      <q-banner v-else-if="original?.tieneReservasActivas" rounded class="bg-teal-1 q-mb-lg">
        <template #avatar><q-icon name="info" color="primary" /></template>
        Esta actividad tiene reservas activas: las fechas están bloqueadas y la capacidad no puede ser menor que los
        {{ original.cuposReservados }} cupos reservados. Los cambios de precio no afectan a las reservas ya hechas.
      </q-banner>
      <EstadoEmpresaBanner v-if="esNueva" :estado="auth.empresa?.estado" :motivo="auth.empresa?.motivoEstado" />

      <div class="formulario">
        <q-form ref="formulario" greedy class="formulario__principal" :class="{ 'no-pointer-events': soloLectura }" @submit="guardar(false)" @validation-error="avisarErrores">
          <!-- Información general -->
          <section class="tg-tarjeta q-pa-lg q-mb-md" aria-labelledby="sec-info">
            <h2 id="sec-info" class="seccion">Información general</h2>
            <q-input v-model.trim="f.nombre" label="Nombre de la actividad" outlined counter maxlength="100" :error="!!err.nombre" :error-message="err.nombre" :rules="[requerido('Ingresa el nombre.'), minLongitud(5)]" lazy-rules hint="Ej.: Camino Inca clásico 4D/3N" />
            <div class="row q-col-gutter-md q-mt-none">
              <q-select
                v-model="f.destino"
                class="col-12 col-sm-6"
                label="Destino"
                outlined
                use-input
                hide-selected
                fill-input
                input-debounce="0"
                new-value-mode="add-unique"
                :options="opcionesDestino"
                :error="!!err.destino"
                :error-message="err.destino"
                :rules="[requerido('Ingresa el destino.'), maxLongitud(80)]"
                lazy-rules
                hint="Región o ciudad (puedes escribir una nueva)"
                @filter="filtrarDestinos"
                @input-value="(v) => (f.destino = v)"
              />
              <q-input v-model.trim="f.puntoEncuentro" class="col-12 col-sm-6" label="Punto de encuentro" outlined :error="!!err.puntoEncuentro" :error-message="err.puntoEncuentro" :rules="[requerido('Ingresa el punto de encuentro.'), maxLongitud(200)]" lazy-rules hint="Lugar y hora de recojo" />
            </div>
            <q-input v-model="f.descripcion" class="q-mt-md" type="textarea" label="Descripción" outlined autogrow counter maxlength="2000" :error="!!err.descripcion" :error-message="err.descripcion" :rules="[requerido('Ingresa una descripción.'), minLongitud(20)]" lazy-rules hint="Qué vivirá el viajero, nivel de dificultad, recomendaciones" />
          </section>

          <!-- Fechas, cupos y precio -->
          <section class="tg-tarjeta q-pa-lg q-mb-md" aria-labelledby="sec-fechas">
            <h2 id="sec-fechas" class="seccion">Fechas, cupos y precio</h2>
            <div class="row q-col-gutter-md">
              <q-input v-model="f.fechaInicio" class="col-12 col-sm-6" type="date" label="Fecha de inicio" stack-label outlined :min="manana" :disable="fechasBloqueadas" :error="!!err.fechaInicio" :error-message="err.fechaInicio" :rules="[requerido('Selecciona la fecha de inicio.'), (v) => fechasBloqueadas || v >= manana || 'Debe ser posterior a hoy.']" />
              <q-input v-model="f.fechaFin" class="col-12 col-sm-6" type="date" label="Fecha de fin" stack-label outlined :min="f.fechaInicio || manana" :disable="fechasBloqueadas" :error="!!err.fechaFin" :error-message="err.fechaFin" :rules="[requerido('Selecciona la fecha de fin.'), (v) => !f.fechaInicio || v >= f.fechaInicio || 'Debe ser igual o posterior al inicio.', () => duracion <= 30 || 'Máximo 30 días.']" :hint="duracion > 0 ? duracionTexto(duracion) : ''" />
              <q-input v-model.number="f.capacidad" class="col-6" type="number" label="Capacidad (personas)" outlined :min="minCapacidad" max="500" :error="!!err.capacidad" :error-message="err.capacidad" :rules="[requerido('Indica la capacidad.'), numeroEntre(minCapacidad, 500, `Entre ${minCapacidad} y 500.`)]" lazy-rules />
              <q-input v-model.number="f.precio" class="col-6" type="number" label="Precio por persona" prefix="S/" outlined min="0.01" step="0.01" :error="!!err.precio" :error-message="err.precio" :rules="[requerido('Indica el precio.'), numeroEntre(0.01, 50000, 'Entre S/ 0.01 y S/ 50 000.')]" lazy-rules />
            </div>
          </section>

          <!-- Itinerario -->
          <section class="tg-tarjeta q-pa-lg q-mb-md" aria-labelledby="sec-itinerario">
            <div class="row items-center justify-between q-mb-sm">
              <h2 id="sec-itinerario" class="seccion q-mb-none">Itinerario</h2>
              <q-btn v-if="f.itinerario.length !== duracion && duracion > 0 && duracion <= 30" flat dense no-caps color="primary" icon="auto_fix_high" :label="`Ajustar a ${pluralizar(duracion, 'día')}`" @click="ajustarItinerario" />
            </div>
            <p class="tg-texto-suave q-mt-none">Describe cada día. Es obligatorio para publicar (al menos un día).</p>
            <p v-if="err.itinerario" class="text-negative text-caption">{{ err.itinerario }}</p>

            <ol class="itinerario">
              <li v-for="(dia, i) in f.itinerario" :key="dia.clave" class="itinerario__dia">
                <div class="row items-center q-mb-sm">
                  <q-select v-model="dia.dia" :options="opcionesDia" emit-value map-options dense outlined label="Día" style="width: 120px" :error="!!err[`itinerario[${i}].dia`] || diaDuplicado(dia.dia)" :error-message="err[`itinerario[${i}]`] || (diaDuplicado(dia.dia) ? 'Día repetido' : '')" />
                  <q-space />
                  <q-btn flat round dense icon="arrow_upward" :disable="i === 0" aria-label="Subir día" @click="mover(i, -1)" />
                  <q-btn flat round dense icon="arrow_downward" :disable="i === f.itinerario.length - 1" aria-label="Bajar día" @click="mover(i, 1)" />
                  <q-btn flat round dense icon="delete_outline" color="negative" :aria-label="`Quitar día ${dia.dia}`" @click="f.itinerario.splice(i, 1)" />
                </div>
                <q-input v-model.trim="dia.titulo" dense outlined label="Título del día" maxlength="100" :error="!!err[`itinerario[${i}].titulo`]" :error-message="err[`itinerario[${i}].titulo`]" :rules="[requerido('Ingresa un título.')]" lazy-rules />
                <q-input v-model="dia.descripcion" dense outlined type="textarea" autogrow label="Actividades del día" maxlength="1000" :error="!!err[`itinerario[${i}].descripcion`]" :error-message="err[`itinerario[${i}].descripcion`]" :rules="[requerido('Describe las actividades del día.')]" lazy-rules />
              </li>
            </ol>
            <q-btn outline no-caps color="primary" icon="add" label="Agregar día" :disable="f.itinerario.length >= 30" @click="agregarDia" />
          </section>

          <!-- Detalles -->
          <section class="tg-tarjeta q-pa-lg q-mb-md" aria-labelledby="sec-detalles">
            <h2 id="sec-detalles" class="seccion">Detalles</h2>
            <div class="row q-col-gutter-md">
              <q-input v-model="f.incluye" class="col-12 col-sm-6" type="textarea" autogrow label="Incluye" outlined counter maxlength="1000" hint="Un elemento por línea o separados por comas" :error="!!err.incluye" :error-message="err.incluye" />
              <q-input v-model="f.noIncluye" class="col-12 col-sm-6" type="textarea" autogrow label="No incluye" outlined counter maxlength="1000" hint="Un elemento por línea o separados por comas" :error="!!err.noIncluye" :error-message="err.noIncluye" />
            </div>
            <q-input v-model.trim="f.imagenUrl" class="q-mt-md" label="URL de imagen de portada (opcional)" outlined :error="!!err.imagenUrl" :error-message="err.imagenUrl" :rules="[url]" lazy-rules hint="Usa una imagen propia o con licencia. Sin imagen se muestra una portada ilustrada." />
          </section>

          <div v-if="!soloLectura" class="acciones">
            <q-btn flat no-caps color="grey-8" label="Cancelar" to="/empresa/actividades" />
            <q-space />
            <q-btn type="submit" outline no-caps color="primary" :label="esNueva ? 'Guardar borrador' : 'Guardar cambios'" :loading="guardando === 'guardar'" :disable="!!guardando" />
            <q-btn v-if="esNueva || original?.estado === 'Borrador'" unelevated no-caps color="primary" icon="public" label="Guardar y publicar" :loading="guardando === 'publicar'" :disable="!!guardando || !auth.empresa || auth.empresa.estado !== 'Verificada'" @click="guardarYPublicar">
              <q-tooltip v-if="auth.empresa?.estado !== 'Verificada'">Podrás publicar cuando tu empresa esté verificada</q-tooltip>
            </q-btn>
          </div>
        </q-form>

        <!-- Vista previa -->
        <aside v-if="$q.screen.gt.sm" class="formulario__vista" aria-label="Vista previa">
          <div class="tg-etiqueta q-mb-sm">Vista previa en el catálogo</div>
          <ActividadCard :actividad="vistaPrevia" />
          <p class="text-caption tg-texto-suave q-mt-sm">Así verán los viajeros tu actividad en los resultados.</p>
        </aside>
      </div>
    </template>
  </q-page>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import ActividadCard from '@/components/ActividadCard.vue'
import EncabezadoPagina from '@/components/EncabezadoPagina.vue'
import EstadoActividadChip from '@/components/EstadoActividadChip.vue'
import EstadoEmpresaBanner from '@/components/EstadoEmpresaBanner.vue'
import EstadoError from '@/components/EstadoError.vue'
import { useAvisos } from '@/composables/useAvisos'
import { catalogoApi, empresaApi } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { erroresDeCampo, mensajeError } from '@/utils/errores'
import { duracionTexto, parsearFecha, pluralizar, sumarDias, hoyIso } from '@/utils/formato'
import { maxLongitud, minLongitud, numeroEntre, requerido, url } from '@/utils/reglas'

const DEPARTAMENTOS = ['Amazonas', 'Áncash', 'Apurímac', 'Arequipa', 'Ayacucho', 'Cajamarca', 'Callao', 'Cusco', 'Huancavelica', 'Huánuco', 'Ica', 'Junín', 'La Libertad', 'Lambayeque', 'Lima', 'Loreto', 'Madre de Dios', 'Moquegua', 'Pasco', 'Piura', 'Puno', 'San Martín', 'Tacna', 'Tumbes', 'Ucayali']

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const avisos = useAvisos()

const esNueva = computed(() => !route.params.id)
const manana = sumarDias(hoyIso(), 1)
let siguienteClave = 1

const vacio = () => ({
  nombre: '', destino: '', puntoEncuentro: '', descripcion: '',
  fechaInicio: sumarDias(hoyIso(), 14), fechaFin: sumarDias(hoyIso(), 14),
  capacidad: 10, precio: null, incluye: '', noIncluye: '', imagenUrl: '',
  itinerario: [{ clave: siguienteClave++, dia: 1, titulo: '', descripcion: '' }],
})

const f = reactive(vacio())
const original = ref(null)
const instantanea = ref('')
const cargando = ref(!esNueva.value)
const errorCarga = ref('')
const guardando = ref(null)
const err = ref({})
const formulario = ref(null)
const destinosConocidos = ref([...DEPARTAMENTOS])
const opcionesDestino = ref(destinosConocidos.value)

const soloLectura = computed(() => !!original.value && (original.value.estado === 'Cancelada' || original.value.fechaInicio <= hoyIso()))
const fechasBloqueadas = computed(() => !!original.value?.tieneReservasActivas || soloLectura.value)
const minCapacidad = computed(() => Math.max(1, original.value?.cuposReservados ?? 1))
const duracion = computed(() => {
  const a = parsearFecha(f.fechaInicio)
  const b = parsearFecha(f.fechaFin)
  return a && b ? Math.round((b - a) / 86400000) + 1 : 0
})
const opcionesDia = computed(() =>
  Array.from({ length: Math.min(30, Math.max(1, duracion.value)) }, (_, i) => ({ label: `Día ${i + 1}`, value: i + 1 })),
)
const sucio = computed(() => JSON.stringify(f) !== instantanea.value)

const vistaPrevia = computed(() => ({
  id: original.value?.id ?? 0,
  nombre: f.nombre || 'Nombre de tu actividad',
  destino: f.destino || 'Destino',
  fechaInicio: f.fechaInicio,
  fechaFin: f.fechaFin || f.fechaInicio,
  duracionDias: Math.max(1, duracion.value),
  precio: Number(f.precio) || 0,
  capacidad: f.capacidad || 0,
  cuposDisponibles: Math.max(0, (f.capacidad || 0) - (original.value?.cuposReservados ?? 0)),
  imagenUrl: /^https?:\/\//.test(f.imagenUrl) ? f.imagenUrl : null,
  valoracion: null,
  totalResenas: 0,
  empresa: { nombreComercial: auth.empresa?.nombreComercial ?? 'Tu empresa', valoracion: null, totalResenas: 0 },
}))

const diaDuplicado = (d) => f.itinerario.filter((x) => x.dia === d).length > 1

function filtrarDestinos(texto, actualizar) {
  actualizar(() => {
    const t = texto.toLowerCase()
    opcionesDestino.value = destinosConocidos.value.filter((d) => d.toLowerCase().includes(t))
  })
}

function agregarDia() {
  const usados = new Set(f.itinerario.map((d) => d.dia))
  const libre = opcionesDia.value.find((o) => !usados.has(o.value))?.value ?? f.itinerario.length + 1
  f.itinerario.push({ clave: siguienteClave++, dia: libre, titulo: '', descripcion: '' })
}

function ajustarItinerario() {
  const porDia = new Map(f.itinerario.map((d) => [d.dia, d]))
  f.itinerario.splice(
    0,
    f.itinerario.length,
    ...Array.from({ length: duracion.value }, (_, i) => porDia.get(i + 1) ?? { clave: siguienteClave++, dia: i + 1, titulo: '', descripcion: '' }),
  )
}

function mover(i, delta) {
  const [d] = f.itinerario.splice(i, 1)
  f.itinerario.splice(i + delta, 0, d)
}

function datosEnvio() {
  return {
    nombre: f.nombre, destino: f.destino?.trim(), puntoEncuentro: f.puntoEncuentro, descripcion: f.descripcion.trim(),
    fechaInicio: f.fechaInicio, fechaFin: f.fechaFin, capacidad: Number(f.capacidad), precio: Number(f.precio),
    incluye: f.incluye || null, noIncluye: f.noIncluye || null, imagenUrl: f.imagenUrl || null,
    itinerario: f.itinerario.map(({ dia, titulo, descripcion }) => ({ dia, titulo, descripcion: descripcion.trim() })),
  }
}

function avisarErrores() {
  avisos.error(null, 'Revisa los campos marcados antes de guardar.')
}

/** Guarda y devuelve el id, o null si falló. */
async function guardarDatos(modo) {
  if (f.itinerario.some((d) => diaDuplicado(d.dia))) {
    avisos.error(null, 'Hay días repetidos en el itinerario.')
    return null
  }
  guardando.value = modo
  err.value = {}
  try {
    const resultado = esNueva.value ? await empresaApi.crear(datosEnvio()) : await empresaApi.actualizar(route.params.id, datosEnvio())
    aplicar(resultado)
    return resultado.id
  } catch (e) {
    err.value = erroresDeCampo(e)
    if (Object.keys(err.value).length) avisos.error(null, 'Revisa los campos marcados.')
    else avisos.error(e)
    return null
  } finally {
    guardando.value = null
  }
}

async function guardar() {
  const id = await guardarDatos('guardar')
  if (!id) return
  avisos.exito(esNueva.value ? 'Borrador guardado.' : 'Cambios guardados.')
  if (esNueva.value) router.replace(`/empresa/actividades/${id}/editar`)
}

async function guardarYPublicar() {
  if (!(await formulario.value.validate())) return avisarErrores()
  const eraNueva = esNueva.value
  const id = await guardarDatos('publicar')
  if (!id) return
  try {
    await empresaApi.publicar(id)
    instantanea.value = JSON.stringify(f)
    avisos.exito('¡Actividad publicada! Ya aparece en el catálogo.')
    router.push('/empresa/actividades')
  } catch (e) {
    avisos.error(e, 'Se guardó como borrador, pero no se pudo publicar.')
    if (eraNueva) router.replace(`/empresa/actividades/${id}/editar`)
  }
}

async function duplicar() {
  try {
    const copia = await empresaApi.duplicar(original.value.id)
    avisos.exito('Se creó una copia como borrador.')
    router.push(`/empresa/actividades/${copia.id}/editar`)
  } catch (e) {
    avisos.error(e)
  }
}

function aplicar(a) {
  original.value = a
  Object.assign(f, {
    nombre: a.nombre, destino: a.destino, puntoEncuentro: a.puntoEncuentro, descripcion: a.descripcion,
    fechaInicio: a.fechaInicio, fechaFin: a.fechaFin, capacidad: a.capacidad, precio: a.precio,
    incluye: a.incluye ?? '', noIncluye: a.noIncluye ?? '', imagenUrl: a.imagenUrl ?? '',
    itinerario: a.itinerario.map((d) => ({ clave: siguienteClave++, ...d })),
  })
  instantanea.value = JSON.stringify(f)
}

async function cargar() {
  if (esNueva.value) {
    instantanea.value = JSON.stringify(f)
    return
  }
  cargando.value = true
  errorCarga.value = ''
  try {
    aplicar(await empresaApi.actividad(route.params.id))
  } catch (e) {
    errorCarga.value = mensajeError(e)
  } finally {
    cargando.value = false
  }
}

onBeforeRouteLeave(async () => {
  if (!sucio.value || soloLectura.value || guardando.value) return true
  return avisos.confirmar({
    titulo: 'Cambios sin guardar',
    mensaje: 'Si sales ahora perderás los cambios que hiciste.',
    textoAceptar: 'Salir sin guardar',
    textoCancelar: 'Seguir editando',
    peligro: true,
  })
})

onMounted(async () => {
  cargar()
  try {
    const existentes = await catalogoApi.destinos()
    destinosConocidos.value = [...new Set([...existentes, ...DEPARTAMENTOS])].sort((a, b) => a.localeCompare(b, 'es'))
    opcionesDestino.value = destinosConocidos.value
  } catch {
    /* se usan los departamentos */
  }
})
</script>

<style scoped lang="scss">
.formulario {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 320px;
  gap: 24px;
  align-items: start;

  @media (max-width: 1023px) {
    grid-template-columns: 1fr;
  }
}

.formulario__vista {
  position: sticky;
  top: 84px;
}

.seccion {
  font-size: 1.1rem;
  font-weight: 600;
  margin: 0 0 14px;
}

.itinerario {
  list-style: none;
  padding: 0;
  margin: 0 0 12px;
  display: grid;
  gap: 12px;
}

.itinerario__dia {
  border: 1px solid var(--tg-borde);
  border-left: 4px solid $primary;
  border-radius: 10px;
  padding: 12px;
  display: grid;
  gap: 4px;
}

.acciones {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
  position: sticky;
  bottom: 0;
  background: var(--tg-fondo);
  padding: 12px 0;
  z-index: 1;
}
</style>
