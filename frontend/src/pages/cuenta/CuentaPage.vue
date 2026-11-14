<template>
  <q-page class="tg-contenedor tg-contenedor--estrecho q-py-lg">
    <EncabezadoPagina titulo="Mi cuenta" subtitulo="Administra tus datos personales y la seguridad de tu cuenta." />

    <section class="tg-tarjeta q-pa-lg q-mb-lg" aria-labelledby="titulo-datos">
      <div class="row items-center q-mb-md">
        <q-avatar size="56px" color="primary" text-color="white" class="text-h6">
          {{ iniciales(auth.usuario.nombre, auth.usuario.apellidos) }}
        </q-avatar>
        <div class="q-ml-md">
          <h2 id="titulo-datos" class="text-h6 q-my-none">Datos personales</h2>
          <div class="tg-texto-suave">{{ auth.usuario.correo }} · {{ etiquetaRol }}</div>
        </div>
      </div>

      <q-form greedy class="q-gutter-y-sm" @submit="guardarDatos">
        <div class="row q-col-gutter-sm">
          <q-input v-model.trim="datos.nombre" class="col-12 col-sm-6" label="Nombres" outlined :error-message="errDatos.nombre" :error="!!errDatos.nombre" :rules="[requerido('Ingresa tus nombres.'), maxLongitud(100)]" lazy-rules />
          <q-input v-model.trim="datos.apellidos" class="col-12 col-sm-6" label="Apellidos" outlined :error-message="errDatos.apellidos" :error="!!errDatos.apellidos" :rules="[requerido('Ingresa tus apellidos.'), maxLongitud(100)]" lazy-rules />
        </div>
        <div class="row q-col-gutter-sm">
          <q-input v-if="auth.esTurista" v-model.trim="datos.dni" class="col-12 col-sm-6" label="DNI" outlined mask="########" inputmode="numeric" :error-message="errDatos.dni" :error="!!errDatos.dni" :rules="[requerido('Ingresa tu DNI.'), reglas.dni]" lazy-rules />
          <q-input v-model.trim="datos.telefono" class="col-12 col-sm-6" label="Teléfono" outlined inputmode="tel" :error-message="errDatos.telefono" :error="!!errDatos.telefono" :rules="[reglas.telefono]" lazy-rules />
        </div>
        <div class="row justify-end">
          <q-btn type="submit" color="primary" unelevated no-caps label="Guardar cambios" :loading="guardandoDatos" :disable="!hayCambios" />
        </div>
      </q-form>
    </section>

    <section class="tg-tarjeta q-pa-lg" aria-labelledby="titulo-seguridad">
      <h2 id="titulo-seguridad" class="text-h6 q-mt-none q-mb-xs">Contraseña</h2>
      <p class="tg-texto-suave q-mb-md">Por seguridad, nunca mostramos tu contraseña. Puedes cambiarla aquí.</p>

      <q-form ref="formPassword" greedy class="q-gutter-y-sm" @submit="cambiarPassword">
        <q-input v-model="pw.actual" label="Contraseña actual" type="password" autocomplete="current-password" outlined :error-message="errPw.passwordActual" :error="!!errPw.passwordActual" :rules="[requerido('Ingresa tu contraseña actual.')]" lazy-rules />
        <div class="row q-col-gutter-sm">
          <q-input v-model="pw.nueva" class="col-12 col-sm-6" label="Nueva contraseña" type="password" autocomplete="new-password" outlined :error-message="errPw.passwordNueva" :error="!!errPw.passwordNueva" :rules="[contrasenaSegura]" lazy-rules />
          <q-input v-model="pw.repetir" class="col-12 col-sm-6" label="Repite la nueva contraseña" type="password" autocomplete="new-password" outlined :rules="[requerido('Repite la contraseña.'), coincideCon(() => pw.nueva)]" lazy-rules />
        </div>
        <div class="row justify-end">
          <q-btn type="submit" color="primary" outline no-caps label="Cambiar contraseña" :loading="guardandoPw" />
        </div>
      </q-form>
    </section>
  </q-page>
</template>

<script setup>
import { computed, reactive, ref } from 'vue'
import EncabezadoPagina from '@/components/EncabezadoPagina.vue'
import { useAuthStore } from '@/stores/auth'
import { useAvisos } from '@/composables/useAvisos'
import { cuentaApi } from '@/services/api'
import { erroresDeCampo } from '@/utils/errores'
import { iniciales } from '@/utils/formato'
import * as reglas from '@/utils/reglas'

const { requerido, maxLongitud, contrasenaSegura, coincideCon } = reglas
const auth = useAuthStore()
const avisos = useAvisos()

const original = () => ({
  nombre: auth.usuario.nombre,
  apellidos: auth.usuario.apellidos,
  dni: auth.usuario.dni ?? '',
  telefono: auth.usuario.telefono ?? '',
})
const datos = reactive(original())
const errDatos = ref({})
const guardandoDatos = ref(false)
const hayCambios = computed(() => JSON.stringify(datos) !== JSON.stringify(original()))
const etiquetaRol = computed(() => ({ Turista: 'Viajero', Empresa: 'Empresa', Admin: 'Administrador' })[auth.rol])

const pw = reactive({ actual: '', nueva: '', repetir: '' })
const errPw = ref({})
const guardandoPw = ref(false)
const formPassword = ref(null)

async function guardarDatos() {
  guardandoDatos.value = true
  errDatos.value = {}
  try {
    const usuario = await cuentaApi.actualizarPerfil({
      nombre: datos.nombre,
      apellidos: datos.apellidos,
      telefono: datos.telefono || null,
      dni: auth.esTurista ? datos.dni : null,
    })
    auth.actualizarUsuario(usuario)
    Object.assign(datos, original())
    avisos.exito('Tus datos se guardaron correctamente.')
  } catch (error) {
    errDatos.value = erroresDeCampo(error)
    if (!Object.keys(errDatos.value).length) avisos.error(error)
  } finally {
    guardandoDatos.value = false
  }
}

async function cambiarPassword() {
  guardandoPw.value = true
  errPw.value = {}
  try {
    await cuentaApi.cambiarPassword(pw.actual, pw.nueva)
    Object.assign(pw, { actual: '', nueva: '', repetir: '' })
    formPassword.value?.resetValidation()
    avisos.exito('Tu contraseña se cambió correctamente.')
  } catch (error) {
    errPw.value = erroresDeCampo(error)
    if (!Object.keys(errPw.value).length) avisos.error(error)
  } finally {
    guardandoPw.value = false
  }
}
</script>
