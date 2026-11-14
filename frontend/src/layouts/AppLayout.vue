<template>
  <q-layout view="hHh lpR fff">
    <a href="#contenido" class="saltar-enlace">Saltar al contenido</a>

    <q-header class="cabecera" height-hint="64">
      <q-toolbar class="tg-contenedor cabecera__barra">
        <q-btn
          v-if="$q.screen.lt.md"
          flat
          round
          dense
          icon="menu"
          aria-label="Abrir menú"
          class="q-mr-xs"
          @click="menuMovil = true"
        />
        <router-link :to="auth.rutaInicio" class="cabecera__logo" aria-label="TurismoGo, ir al inicio">
          <LogoMarca />
        </router-link>

        <nav v-if="$q.screen.gt.sm" class="cabecera__nav" aria-label="Navegación principal">
          <q-btn
            v-for="item in menu"
            :key="item.to"
            :to="item.to"
            :label="item.label"
            flat
            no-caps
            class="cabecera__enlace"
            :class="{ 'cabecera__enlace--activo': estaActivo(item) }"
          />
        </nav>

        <q-space />

        <template v-if="!auth.autenticado">
          <q-btn flat no-caps color="primary" label="Ingresar" :to="rutaIngresar" />
          <q-btn
            v-if="$q.screen.gt.xs"
            unelevated
            no-caps
            color="primary"
            label="Crear cuenta"
            to="/registro"
            class="q-ml-sm"
          />
        </template>

        <q-btn v-else flat no-caps class="cabecera__usuario" :aria-label="`Cuenta de ${auth.usuario.nombre}`">
          <q-avatar size="34px" color="primary" text-color="white" class="text-weight-medium">
            {{ iniciales(auth.usuario.nombre, auth.usuario.apellidos) }}
          </q-avatar>
          <span v-if="$q.screen.gt.xs" class="q-ml-sm text-left">
            <span class="block text-body2 text-weight-medium">{{ auth.nombreCorto }}</span>
            <span class="block text-caption tg-texto-suave">{{ etiquetaRol }}</span>
          </span>
          <q-icon name="expand_more" size="20px" class="q-ml-xs" aria-hidden="true" />

          <q-menu anchor="bottom right" self="top right" :offset="[0, 6]">
            <q-list style="min-width: 220px">
              <q-item>
                <q-item-section>
                  <q-item-label class="text-weight-medium">{{ auth.usuario.nombre }} {{ auth.usuario.apellidos }}</q-item-label>
                  <q-item-label caption>{{ auth.usuario.correo }}</q-item-label>
                </q-item-section>
              </q-item>
              <q-separator />
              <q-item v-close-popup clickable to="/cuenta">
                <q-item-section avatar><q-icon name="manage_accounts" /></q-item-section>
                <q-item-section>Mi cuenta</q-item-section>
              </q-item>
              <q-item v-if="auth.esEmpresa" v-close-popup clickable to="/empresa/perfil">
                <q-item-section avatar><q-icon name="storefront" /></q-item-section>
                <q-item-section>Perfil de empresa</q-item-section>
              </q-item>
              <q-separator />
              <q-item v-close-popup clickable @click="cerrarSesion">
                <q-item-section avatar><q-icon name="logout" /></q-item-section>
                <q-item-section>Cerrar sesión</q-item-section>
              </q-item>
            </q-list>
          </q-menu>
        </q-btn>
      </q-toolbar>
    </q-header>

    <!-- Menú lateral en móvil -->
    <q-drawer v-model="menuMovil" overlay behavior="mobile" :width="290" bordered>
      <div class="q-pa-md">
        <LogoMarca />
      </div>
      <q-list padding aria-label="Navegación principal">
        <q-item
          v-for="item in menu"
          :key="item.to"
          v-ripple
          clickable
          :to="item.to"
          :active="estaActivo(item)"
          active-class="menu-activo"
          @click="menuMovil = false"
        >
          <q-item-section avatar><q-icon :name="item.icono" /></q-item-section>
          <q-item-section>{{ item.label }}</q-item-section>
        </q-item>
        <template v-if="!auth.autenticado">
          <q-separator class="q-my-sm" />
          <q-item v-ripple clickable :to="rutaIngresar" @click="menuMovil = false">
            <q-item-section avatar><q-icon name="login" /></q-item-section>
            <q-item-section>Ingresar</q-item-section>
          </q-item>
          <q-item v-ripple clickable to="/registro" @click="menuMovil = false">
            <q-item-section avatar><q-icon name="person_add" /></q-item-section>
            <q-item-section>Crear cuenta</q-item-section>
          </q-item>
        </template>
      </q-list>
    </q-drawer>

    <q-page-container>
      <main id="contenido" tabindex="-1">
        <router-view />
      </main>
    </q-page-container>

    <footer class="pie">
      <div class="tg-contenedor pie__contenido">
        <div>
          <LogoMarca claro />
          <p class="pie__texto">Actividades turísticas en Perú con operadores verificados.</p>
        </div>
        <nav class="pie__enlaces" aria-label="Enlaces del pie de página">
          <router-link to="/actividades">Explorar actividades</router-link>
          <router-link v-if="!auth.autenticado" to="/registro?tipo=empresa">Publica tus actividades</router-link>
        </nav>
      </div>
      <div class="tg-contenedor pie__legal">© {{ anio }} TurismoGo. Proyecto de código abierto bajo licencia MIT.</div>
    </footer>
  </q-layout>
</template>

<script setup>
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import LogoMarca from '@/components/LogoMarca.vue'
import { useAuthStore } from '@/stores/auth'
import { useAvisos } from '@/composables/useAvisos'
import { iniciales } from '@/utils/formato'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const avisos = useAvisos()
const menuMovil = ref(false)
const anio = new Date().getFullYear()

const menus = {
  invitado: [{ label: 'Explorar', to: '/actividades', icono: 'travel_explore' }],
  Turista: [
    { label: 'Explorar', to: '/actividades', icono: 'travel_explore' },
    { label: 'Mis reservas', to: '/mis-reservas', icono: 'confirmation_number' },
  ],
  Empresa: [
    { label: 'Panel', to: '/empresa', icono: 'dashboard', exacto: true },
    { label: 'Mis actividades', to: '/empresa/actividades', icono: 'event_note' },
    { label: 'Reservas', to: '/empresa/reservas', icono: 'groups' },
  ],
  Admin: [
    { label: 'Panel', to: '/admin', icono: 'insights', exacto: true },
    { label: 'Empresas', to: '/admin/empresas', icono: 'storefront' },
    { label: 'Usuarios', to: '/admin/usuarios', icono: 'people' },
    { label: 'Reseñas', to: '/admin/resenas', icono: 'reviews' },
    { label: 'Catálogo', to: '/actividades', icono: 'travel_explore' },
  ],
}

const menu = computed(() => menus[auth.rol] ?? menus.invitado)
const etiquetaRol = computed(() => ({ Turista: 'Viajero', Empresa: auth.empresa?.nombreComercial ?? 'Empresa', Admin: 'Administrador' })[auth.rol])
const rutaIngresar = computed(() =>
  route.path.startsWith('/actividades/') ? { path: '/ingresar', query: { redirect: route.fullPath } } : '/ingresar',
)

const estaActivo = (item) => (item.exacto ? route.path === item.to : route.path.startsWith(item.to))

async function cerrarSesion() {
  const ok = await avisos.confirmar({
    titulo: 'Cerrar sesión',
    mensaje: '¿Quieres cerrar tu sesión en este dispositivo?',
    textoAceptar: 'Cerrar sesión',
  })
  if (!ok) return
  auth.cerrarSesion()
  avisos.info('Cerraste sesión. ¡Hasta pronto!')
  router.push('/')
}
</script>

<style scoped lang="scss">
.saltar-enlace {
  position: absolute;
  left: -9999px;
  top: 8px;
  z-index: 3000;
  background: #fff;
  padding: 8px 12px;
  border-radius: 8px;

  &:focus {
    left: 8px;
  }
}

.cabecera {
  background: rgba(255, 255, 255, 0.96);
  backdrop-filter: blur(8px);
  color: #0f172a;
  border-bottom: 1px solid var(--tg-borde);
}

.cabecera__barra {
  min-height: 64px;
}

.cabecera__logo {
  text-decoration: none;
  display: inline-flex;
}

.cabecera__nav {
  display: flex;
  gap: 4px;
  margin-left: 28px;
}

.cabecera__enlace {
  color: var(--tg-texto-suave);
  font-weight: 500;
  border-radius: 10px;

  &--activo {
    color: $primary;
    background: rgba(15, 118, 110, 0.08);
  }
}

.cabecera__usuario {
  padding: 4px 6px;
  border-radius: 12px;
}

.menu-activo {
  color: $primary;
  background: rgba(15, 118, 110, 0.08);
  font-weight: 500;
}

main:focus {
  outline: none;
}

.pie {
  background: #0f172a;
  color: #cbd5e1;
  margin-top: 48px;
  padding: 32px 0 20px;
}

.pie__contenido {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  gap: 20px;
}

.pie__texto {
  margin: 10px 0 0;
  max-width: 340px;
}

.pie__enlaces {
  display: flex;
  flex-direction: column;
  gap: 8px;

  a {
    color: #e2e8f0;
    text-decoration: none;

    &:hover {
      text-decoration: underline;
    }
  }
}

.pie__legal {
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px solid #1e293b;
  font-size: 0.85rem;
  color: #94a3b8;
}
</style>
