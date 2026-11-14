<template>
  <router-view />
</template>

<script setup>
import { useQuasar } from 'quasar'
import { useRouter } from 'vue-router'

defineOptions({ name: 'App' })

// Barra de progreso durante la navegación entre páginas
const $q = useQuasar()
const router = useRouter()
router.beforeEach(() => {
  $q.loadingBar.start()
})
router.afterEach(() => {
  $q.loadingBar.stop()
})
router.onError(() => {
  $q.loadingBar.stop()
})
</script>
