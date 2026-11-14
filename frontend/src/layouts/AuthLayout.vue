<template>
  <q-layout view="hHh lpR fff">
    <q-page-container>
      <q-page class="acceso">
        <aside v-if="$q.screen.gt.sm" class="acceso__marca" aria-hidden="true">
          <router-link to="/" class="acceso__logo" tabindex="-1"><LogoMarca claro /></router-link>
          <div>
            <h2 class="acceso__lema">Vive el Perú con operadores de confianza</h2>
            <ul class="acceso__beneficios">
              <li><q-icon name="verified" /> Empresas verificadas por nuestro equipo</li>
              <li><q-icon name="event_available" /> Cupos y precios siempre actualizados</li>
              <li><q-icon name="reviews" /> Reseñas de viajeros que sí asistieron</li>
            </ul>
          </div>
          <svg class="acceso__montes" viewBox="0 0 600 200" preserveAspectRatio="none">
            <path d="M0 200 L110 80 L190 140 L300 30 L420 130 L500 70 L600 150 L600 200 Z" fill="rgba(255,255,255,0.10)" />
            <path d="M0 200 L140 130 L260 175 L380 115 L520 165 L600 140 L600 200 Z" fill="rgba(255,255,255,0.16)" />
          </svg>
        </aside>

        <main class="acceso__formulario">
          <router-link v-if="$q.screen.lt.md" to="/" class="q-mb-lg inline-block" aria-label="TurismoGo, ir al inicio">
            <LogoMarca />
          </router-link>
          <div class="acceso__caja">
            <router-view />
          </div>
        </main>
      </q-page>
    </q-page-container>
  </q-layout>
</template>

<script setup>
import LogoMarca from '@/components/LogoMarca.vue'
</script>

<style scoped lang="scss">
.acceso {
  display: flex;
  min-height: 100vh;
}

.acceso__marca {
  position: relative;
  flex: 0 0 42%;
  max-width: 560px;
  background: linear-gradient(160deg, #0f766e 0%, #134e4a 60%, #0f172a 100%);
  color: #fff;
  padding: 40px 48px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  overflow: hidden;
}

.acceso__logo {
  text-decoration: none;
}

.acceso__lema {
  font-size: 2rem;
  line-height: 1.25;
  font-weight: 600;
  margin: 0 0 20px;
}

.acceso__beneficios {
  list-style: none;
  padding: 0;
  margin: 0;
  display: grid;
  gap: 12px;
  font-size: 1.02rem;
  position: relative;
  z-index: 1;

  .q-icon {
    color: #fde68a;
    margin-right: 8px;
  }
}

.acceso__montes {
  position: absolute;
  left: 0;
  right: 0;
  bottom: 0;
  width: 100%;
  height: 180px;
}

.acceso__formulario {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 32px 16px;
}

.acceso__caja {
  width: 100%;
  max-width: 460px;
}
</style>
