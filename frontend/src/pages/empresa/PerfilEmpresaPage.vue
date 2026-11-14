<template>
  <q-page class="tg-contenedor tg-contenedor--estrecho q-py-lg">
    <EncabezadoPagina titulo="Perfil de empresa" subtitulo="Esta información la ven los viajeros en tus actividades." />

    <div v-if="cargando" class="q-gutter-y-md"><q-skeleton height="320px" class="rounded-borders" /></div>
    <EstadoError v-else-if="error" :mensaje="error" @reintentar="cargar" />

    <template v-else>
      <EstadoEmpresaBanner :estado="perfil.estado" :motivo="perfil.motivoEstado" />
      <q-banner v-if="perfil.estado === 'Verificada'" rounded class="bg-green-1 q-mb-lg">
        <template #avatar><q-icon name="verified" color="positive" /></template>
        Empresa verificada<template v-if="perfil.verificadaEn"> el {{ formatFechaHora(perfil.verificadaEn).split(',')[0] }}</template>.
        Tus actividades muestran el sello de verificación.
      </q-banner>

      <q-form greedy class="tg-tarjeta q-pa-lg" @submit="guardar">
        <h2 class="text-h6 q-mt-none q-mb-md">Datos legales</h2>
        <div class="row q-col-gutter-md">
          <q-input v-model.trim="f.razonSocial" class="col-12 col-sm-8" label="Razón social" outlined :disable="!perfil.puedeEditarDatosLegales" :error="!!err.razonSocial" :error-message="err.razonSocial" :rules="[requerido('Ingresa la razón social.'), maxLongitud(150)]" lazy-rules />
          <q-input v-model.trim="f.ruc" class="col-12 col-sm-4" label="RUC" outlined mask="###########" :disable="!perfil.puedeEditarDatosLegales" :error="!!err.ruc" :error-message="err.ruc" :rules="[requerido('Ingresa el RUC.'), reglas.ruc]" lazy-rules />
        </div>
        <p v-if="!perfil.puedeEditarDatosLegales" class="text-caption tg-texto-suave q-mt-xs">
          <q-icon name="lock" /> La razón social y el RUC no se pueden cambiar después de la verificación. Si necesitas
          corregirlos, contacta al equipo de TurismoGo.
        </p>

        <h2 class="text-h6 q-mt-lg q-mb-md">Información pública</h2>
        <div class="row q-col-gutter-md">
          <q-input v-model.trim="f.nombreComercial" class="col-12 col-sm-6" label="Nombre comercial" outlined :error="!!err.nombreComercial" :error-message="err.nombreComercial" :rules="[requerido('Ingresa el nombre comercial.'), maxLongitud(100)]" lazy-rules />
          <q-input v-model.trim="f.telefono" class="col-12 col-sm-6" label="Teléfono de contacto" outlined inputmode="tel" :error="!!err.telefono" :error-message="err.telefono" :rules="[requerido('Ingresa un teléfono.'), reglas.telefono]" lazy-rules hint="Lo ven los viajeros con reserva" />
        </div>
        <q-input v-model.trim="f.direccion" class="q-mt-md" label="Dirección" outlined :error="!!err.direccion" :error-message="err.direccion" :rules="[requerido('Ingresa la dirección.'), maxLongitud(200)]" lazy-rules />
        <q-input v-model="f.descripcion" class="q-mt-md" type="textarea" autogrow label="Descripción de la empresa" outlined counter maxlength="1000" :error="!!err.descripcion" :error-message="err.descripcion" hint="Experiencia, especialidades, certificaciones" />
        <q-input v-model.trim="f.logoUrl" class="q-mt-md" label="URL del logo (opcional)" outlined :error="!!err.logoUrl" :error-message="err.logoUrl" :rules="[reglas.url]" lazy-rules />

        <div class="row justify-end q-mt-lg">
          <q-btn type="submit" unelevated no-caps color="primary" label="Guardar cambios" :loading="guardando" :disable="!sucio" />
        </div>
      </q-form>
    </template>
  </q-page>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import EncabezadoPagina from '@/components/EncabezadoPagina.vue'
import EstadoEmpresaBanner from '@/components/EstadoEmpresaBanner.vue'
import EstadoError from '@/components/EstadoError.vue'
import { useAvisos } from '@/composables/useAvisos'
import { authApi, empresaApi } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { erroresDeCampo, mensajeError } from '@/utils/errores'
import { formatFechaHora } from '@/utils/formato'
import * as reglas from '@/utils/reglas'

const { requerido, maxLongitud } = reglas
const auth = useAuthStore()
const avisos = useAvisos()

const perfil = ref(null)
const f = reactive({ razonSocial: '', ruc: '', nombreComercial: '', telefono: '', direccion: '', descripcion: '', logoUrl: '' })
const instantanea = ref('')
const cargando = ref(true)
const guardando = ref(false)
const error = ref('')
const err = ref({})
const sucio = computed(() => JSON.stringify(f) !== instantanea.value)

function aplicar(p) {
  perfil.value = p
  Object.assign(f, {
    razonSocial: p.razonSocial, ruc: p.ruc, nombreComercial: p.nombreComercial, telefono: p.telefono,
    direccion: p.direccion, descripcion: p.descripcion ?? '', logoUrl: p.logoUrl ?? '',
  })
  instantanea.value = JSON.stringify(f)
}

async function cargar() {
  cargando.value = true
  error.value = ''
  try {
    aplicar(await empresaApi.perfil())
  } catch (e) {
    error.value = mensajeError(e)
  } finally {
    cargando.value = false
  }
}

async function guardar() {
  guardando.value = true
  err.value = {}
  try {
    aplicar(
      await empresaApi.actualizarPerfil({
        ...f,
        descripcion: f.descripcion || null,
        logoUrl: f.logoUrl || null,
        razonSocial: perfil.value.puedeEditarDatosLegales ? f.razonSocial : null,
        ruc: perfil.value.puedeEditarDatosLegales ? f.ruc : null,
      }),
    )
    auth.actualizarUsuario(await authApi.me()) // el nombre comercial se muestra en la cabecera
    avisos.exito('Perfil de empresa actualizado.')
  } catch (e) {
    err.value = erroresDeCampo(e)
    if (!Object.keys(err.value).length) avisos.error(e)
  } finally {
    guardando.value = false
  }
}

onMounted(cargar)
</script>
