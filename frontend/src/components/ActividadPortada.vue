<template>
  <div class="portada" :style="{ height: alto, background: imagenValida ? undefined : fondo }">
    <q-img
      v-if="imagenValida"
      :src="imagenUrl"
      :alt="alt"
      :ratio="undefined"
      fit="cover"
      class="fit"
      loading="lazy"
      @error="falloImagen = true"
    />
    <template v-else>
      <!-- Portada ilustrada generada a partir del destino (sin imágenes de terceros) -->
      <svg class="portada__montes" viewBox="0 0 400 120" preserveAspectRatio="none" aria-hidden="true">
        <path d="M0 120 L70 55 L120 90 L190 25 L260 85 L320 45 L400 100 L400 120 Z" fill="rgba(255,255,255,0.16)" />
        <path d="M0 120 L90 80 L160 105 L240 70 L330 100 L400 85 L400 120 Z" fill="rgba(255,255,255,0.22)" />
      </svg>
      <div class="portada__sol" />
      <div class="portada__texto">
        <q-icon :name="icono" size="28px" />
        <span>{{ destino }}</span>
      </div>
    </template>
    <slot />
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'

const props = defineProps({
  destino: { type: String, default: '' },
  imagenUrl: { type: String, default: null },
  alt: { type: String, default: '' },
  alto: { type: String, default: '180px' },
})

const falloImagen = ref(false)
const imagenValida = computed(() => !!props.imagenUrl && !falloImagen.value)

// Paletas inspiradas en paisajes peruanos (sierra, costa, selva, altiplano)
const paletas = [
  ['#0f766e', '#115e59'],
  ['#c2410c', '#9a3412'],
  ['#0369a1', '#075985'],
  ['#15803d', '#166534'],
  ['#7c2d12', '#b45309'],
  ['#4338ca', '#3730a3'],
  ['#be123c', '#9f1239'],
]

const hash = (texto) => [...texto].reduce((h, c) => (h * 31 + c.charCodeAt(0)) >>> 0, 7)

const fondo = computed(() => {
  const [a, b] = paletas[hash(props.destino || 'TurismoGo') % paletas.length]
  return `linear-gradient(135deg, ${a} 0%, ${b} 100%)`
})

const icono = computed(() => {
  const d = (props.destino || '').toLowerCase()
  if (/(ica|paracas|lima|piura|tumbes|mancora)/.test(d)) return 'beach_access'
  if (/(loreto|iquitos|madre de dios|tambopata|selva|ucayali)/.test(d)) return 'forest'
  if (/(puno|titicaca)/.test(d)) return 'sailing'
  return 'landscape'
})
</script>

<style scoped lang="scss">
.portada {
  position: relative;
  overflow: hidden;
  color: #fff;
}

.portada__montes {
  position: absolute;
  inset: auto 0 0 0;
  width: 100%;
  height: 65%;
}

.portada__sol {
  position: absolute;
  top: 18%;
  right: 14%;
  width: 46px;
  height: 46px;
  border-radius: 50%;
  background: rgba(253, 230, 138, 0.85);
}

.portada__texto {
  position: absolute;
  left: 16px;
  bottom: 14px;
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 1.05rem;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.25);
}
</style>
