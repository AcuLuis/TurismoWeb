<template>
  <div>
    <h1 class="tg-titulo-pagina">Crea tu cuenta</h1>
    <p class="tg-subtitulo q-mb-md">Es gratis y solo toma un minuto.</p>

    <q-tabs
      v-model="tipo"
      class="selector-tipo q-mb-lg"
      active-class="selector-tipo__activo"
      indicator-color="transparent"
      no-caps
      dense
      align="justify"
    >
      <q-tab name="turista" icon="backpack" label="Soy viajero" />
      <q-tab name="empresa" icon="storefront" label="Soy una empresa" />
    </q-tabs>

    <q-banner v-if="tipo === 'empresa'" class="bg-teal-1 q-mb-md" rounded>
      <template #avatar><q-icon name="verified_user" color="primary" /></template>
      Tras registrarte, nuestro equipo verificará tu RUC. Podrás preparar tus actividades desde ya y publicarlas
      cuando la verificación esté lista.
    </q-banner>

    <q-banner v-if="errorGeneral" class="bg-red-1 text-negative q-mb-md" rounded role="alert">
      <template #avatar><q-icon name="error_outline" /></template>
      {{ errorGeneral }}
    </q-banner>

    <q-form ref="formulario" greedy class="column q-gutter-y-sm" @submit="registrar">
      <div v-if="tipo === 'empresa'" class="tg-etiqueta q-mt-xs">Datos de la empresa</div>
      <template v-if="tipo === 'empresa'">
        <q-input v-model.trim="f.nombreComercial" label="Nombre comercial" outlined :error="!!errores.nombreComercial" :error-message="errores.nombreComercial" :rules="[requerido('Ingresa el nombre comercial.'), maxLongitud(100)]" lazy-rules />
        <q-input v-model.trim="f.razonSocial" label="Razón social" outlined :error="!!errores.razonSocial" :error-message="errores.razonSocial" :rules="[requerido('Ingresa la razón social.'), maxLongitud(150)]" lazy-rules />
        <div class="row q-col-gutter-sm">
          <q-input v-model.trim="f.ruc" class="col-12 col-sm-6" label="RUC" outlined inputmode="numeric" mask="###########" :error="!!errores.ruc" :error-message="errores.ruc" :rules="[requerido('Ingresa el RUC.'), reglas.ruc]" lazy-rules />
          <q-input v-model.trim="f.telefono" class="col-12 col-sm-6" label="Teléfono" outlined inputmode="tel" :error="!!errores.telefono" :error-message="errores.telefono" :rules="[requerido('Ingresa un teléfono.'), reglas.telefono]" lazy-rules />
        </div>
        <q-input v-model.trim="f.direccion" label="Dirección" outlined :error="!!errores.direccion" :error-message="errores.direccion" :rules="[requerido('Ingresa la dirección.'), maxLongitud(200)]" lazy-rules />
        <div class="tg-etiqueta q-mt-sm">Representante</div>
      </template>

      <div class="row q-col-gutter-sm">
        <q-input v-model.trim="f.nombre" class="col-12 col-sm-6" label="Nombres" autocomplete="given-name" outlined :error="!!errores.nombre" :error-message="errores.nombre" :rules="[requerido('Ingresa tus nombres.'), maxLongitud(100)]" lazy-rules />
        <q-input v-model.trim="f.apellidos" class="col-12 col-sm-6" label="Apellidos" autocomplete="family-name" outlined :error="!!errores.apellidos" :error-message="errores.apellidos" :rules="[requerido('Ingresa tus apellidos.'), maxLongitud(100)]" lazy-rules />
      </div>

      <div v-if="tipo === 'turista'" class="row q-col-gutter-sm">
        <q-input v-model.trim="f.dni" class="col-12 col-sm-6" label="DNI" outlined inputmode="numeric" mask="########" hint="Lo usan los operadores para tu registro de pasajero" :error="!!errores.dni" :error-message="errores.dni" :rules="[requerido('Ingresa tu DNI.'), reglas.dni]" lazy-rules />
        <q-input v-model.trim="f.telefono" class="col-12 col-sm-6" label="Celular (opcional)" outlined inputmode="tel" autocomplete="tel" :error="!!errores.telefono" :error-message="errores.telefono" :rules="[reglas.telefono]" lazy-rules />
      </div>

      <q-input v-model.trim="f.correo" label="Correo electrónico" type="email" autocomplete="email" outlined :error="!!errores.correo" :error-message="errores.correo" :rules="[requerido('Ingresa tu correo.'), reglas.correo]" lazy-rules />

      <q-input
        v-model="f.password"
        label="Contraseña"
        :type="verPassword ? 'text' : 'password'"
        autocomplete="new-password"
        outlined
        :error="!!errores.password"
        :error-message="errores.password"
        :rules="[contrasenaSegura]"
        lazy-rules
        hint="Mínimo 8 caracteres, con letras y números"
      >
        <template #append>
          <q-btn flat round dense :icon="verPassword ? 'visibility_off' : 'visibility'" :aria-label="verPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'" @click="verPassword = !verPassword" />
        </template>
      </q-input>
      <div v-if="f.password" class="fuerza" aria-live="polite">
        <q-linear-progress :value="fuerza.valor" :color="fuerza.color" rounded size="6px" />
        <span class="text-caption" :class="`text-${fuerza.color}`">Seguridad: {{ fuerza.texto }}</span>
      </div>

      <q-input
        v-model="f.repetir"
        label="Repite la contraseña"
        :type="verPassword ? 'text' : 'password'"
        autocomplete="new-password"
        outlined
        :rules="[requerido('Repite la contraseña.'), coincideCon(() => f.password)]"
        lazy-rules
      />

      <q-btn type="submit" color="primary" unelevated no-caps size="lg" class="full-width q-mt-md" :label="tipo === 'empresa' ? 'Registrar empresa' : 'Crear cuenta'" :loading="enviando" />
    </q-form>

    <p class="text-center q-mt-lg q-mb-none">
      ¿Ya tienes cuenta?
      <router-link :to="{ path: '/ingresar', query: redirect ? { redirect } : {} }" class="text-primary text-weight-medium">Ingresa aquí</router-link>
    </p>
  </div>
</template>

<script setup>
import { computed, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useAvisos } from '@/composables/useAvisos'
import { erroresDeCampo, mensajeError } from '@/utils/errores'
import * as reglas from '@/utils/reglas'

const { requerido, maxLongitud, contrasenaSegura, coincideCon, fuerzaContrasena } = reglas
const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const avisos = useAvisos()

const tipo = ref(route.query.tipo === 'empresa' ? 'empresa' : 'turista')
const formulario = ref(null)
const enviando = ref(false)
const verPassword = ref(false)
const errorGeneral = ref('')
const errores = ref({})
const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : null

const f = reactive({
  nombre: '', apellidos: '', dni: '', telefono: '', correo: '', password: '', repetir: '',
  razonSocial: '', nombreComercial: '', ruc: '', direccion: '',
})

watch(tipo, () => {
  errores.value = {}
  errorGeneral.value = ''
  formulario.value?.resetValidation()
  router.replace({ query: { ...route.query, tipo: tipo.value } })
})

// Al editar un campo se limpia su error del servidor
watch(f, () => {
  if (Object.keys(errores.value).length) errores.value = {}
})

const fuerza = computed(() => {
  const p = fuerzaContrasena(f.password)
  return [
    { valor: 0.15, color: 'negative', texto: 'muy débil' },
    { valor: 0.35, color: 'negative', texto: 'débil' },
    { valor: 0.6, color: 'warning', texto: 'aceptable' },
    { valor: 0.85, color: 'positive', texto: 'buena' },
    { valor: 1, color: 'positive', texto: 'excelente' },
  ][p]
})

async function registrar() {
  enviando.value = true
  errorGeneral.value = ''
  errores.value = {}
  try {
    if (tipo.value === 'empresa') {
      await auth.registrarEmpresa({
        nombre: f.nombre, apellidos: f.apellidos, correo: f.correo, telefono: f.telefono, password: f.password,
        razonSocial: f.razonSocial, nombreComercial: f.nombreComercial, ruc: f.ruc, direccion: f.direccion,
      })
      avisos.exito('¡Empresa registrada! Ya puedes preparar tus actividades.')
    } else {
      await auth.registrarTurista({
        nombre: f.nombre, apellidos: f.apellidos, dni: f.dni, correo: f.correo,
        telefono: f.telefono || null, password: f.password,
      })
      avisos.exito(`¡Bienvenido a TurismoGo, ${auth.nombreCorto}!`)
    }
    const r = redirect && redirect.startsWith('/') && !redirect.startsWith('//') ? redirect : null
    router.replace(r ?? auth.rutaInicio)
  } catch (error) {
    errores.value = erroresDeCampo(error)
    errorGeneral.value = Object.keys(errores.value).length ? 'Revisa los campos marcados.' : mensajeError(error)
  } finally {
    enviando.value = false
  }
}
</script>

<style scoped lang="scss">
.selector-tipo {
  background: #f1f5f9;
  border-radius: 12px;
  padding: 4px;

  :deep(.q-tab) {
    border-radius: 9px;
    min-height: 44px;
  }
}

:deep(.selector-tipo__activo) {
  background: #fff;
  color: $primary;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.12);
}

.fuerza {
  display: grid;
  gap: 4px;
  margin-top: -4px;
}
</style>
