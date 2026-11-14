<template>
  <main class="no-encontrado">
    <LogoMarca class="q-mb-xl" />
    <div class="no-encontrado__codigo" aria-hidden="true">404</div>
    <h1 class="tg-titulo-pagina">No encontramos esta página</h1>
    <p class="tg-subtitulo q-mb-lg">Es posible que el enlace esté mal escrito o que la página ya no exista.</p>
    <div class="row justify-center q-gutter-sm">
      <q-btn color="primary" unelevated no-caps icon="home" label="Ir al inicio" :to="auth.rutaInicio" />
      <q-btn color="primary" outline no-caps icon="travel_explore" label="Explorar actividades" to="/actividades" />
    </div>
  </main>
</template>

<script setup>
import LogoMarca from '@/components/LogoMarca.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
</script>

<style scoped>
.no-encontrado {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 24px 16px;
}

.no-encontrado__codigo {
  font-size: clamp(5rem, 18vw, 9rem);
  font-weight: 700;
  line-height: 1;
  color: rgba(15, 118, 110, 0.18);
}
</style>
