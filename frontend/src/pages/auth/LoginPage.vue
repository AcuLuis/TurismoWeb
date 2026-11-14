<template>
  <div>
    <h1 class="tg-titulo-pagina">Te damos la bienvenida</h1>
    <p class="tg-subtitulo q-mb-lg">Ingresa con tu correo para gestionar tus reservas o tu empresa.</p>

    <q-banner v-if="errorGeneral" class="bg-red-1 text-negative q-mb-md" rounded role="alert">
      <template #avatar><q-icon name="error_outline" /></template>
      {{ errorGeneral }}
    </q-banner>

    <q-form class="q-gutter-y-md" greedy @submit="ingresar">
      <q-input
        v-model.trim="correo"
        label="Correo electrónico"
        type="email"
        autocomplete="email"
        outlined
        autofocus
        :rules="[requerido('Ingresa tu correo.'), reglas.correo]"
        lazy-rules
      >
        <template #prepend><q-icon name="mail_outline" /></template>
      </q-input>

      <q-input
        v-model="password"
        label="Contraseña"
        :type="verPassword ? 'text' : 'password'"
        autocomplete="current-password"
        outlined
        :rules="[requerido('Ingresa tu contraseña.')]"
        lazy-rules
      >
        <template #prepend><q-icon name="lock_outline" /></template>
        <template #append>
          <q-btn
            flat
            round
            dense
            :icon="verPassword ? 'visibility_off' : 'visibility'"
            :aria-label="verPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'"
            @click="verPassword = !verPassword"
          />
        </template>
      </q-input>

      <q-btn
        type="submit"
        color="primary"
        unelevated
        no-caps
        size="lg"
        class="full-width"
        label="Ingresar"
        :loading="enviando"
      />
    </q-form>

    <p class="text-center q-mt-lg q-mb-none">
      ¿No tienes una cuenta?
      <router-link :to="{ path: '/registro', query: $route.query }" class="text-primary text-weight-medium">Regístrate gratis</router-link>
    </p>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useAvisos } from '@/composables/useAvisos'
import { mensajeError } from '@/utils/errores'
import * as reglas from '@/utils/reglas'

const { requerido } = reglas
const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const avisos = useAvisos()

const correo = ref('')
const password = ref('')
const verPassword = ref(false)
const enviando = ref(false)
const errorGeneral = ref('')

/** Solo se permiten redirecciones internas (evita redirecciones abiertas). */
function destinoSeguro() {
  const r = route.query.redirect
  return typeof r === 'string' && r.startsWith('/') && !r.startsWith('//') ? r : null
}

async function ingresar() {
  enviando.value = true
  errorGeneral.value = ''
  try {
    await auth.login(correo.value, password.value)
    avisos.exito(`¡Hola, ${auth.nombreCorto}!`)
    router.replace(destinoSeguro() ?? auth.rutaInicio)
  } catch (error) {
    errorGeneral.value = mensajeError(error)
  } finally {
    enviando.value = false
  }
}
</script>
