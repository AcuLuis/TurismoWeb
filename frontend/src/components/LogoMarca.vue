<template>
  <span class="logo" :class="{ 'logo--claro': claro }">
    <svg class="logo__icono" viewBox="0 0 64 64" aria-hidden="true" focusable="false">
      <rect width="64" height="64" rx="14" :fill="claro ? '#ffffff' : '#0f766e'" />
      <circle cx="44" cy="20" r="7" fill="#f59e0b" />
      <path d="M6 50 L24 24 L34 38 L41 29 L58 50 Z" :fill="claro ? '#0f766e' : '#ffffff'" />
      <path d="M24 24 L29 31 L26 30 L22 33 Z" :fill="claro ? '#5eead4' : '#99f6e4'" />
    </svg>
    <span class="logo__texto">Turismo<strong>Go</strong></span>
  </span>
</template>

<script setup>
defineProps({
  /** Versión para fondos oscuros */
  claro: { type: Boolean, default: false },
})
</script>

<style scoped lang="scss">
.logo {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  color: #0f172a;
  font-size: 1.25rem;
  letter-spacing: -0.01em;
  white-space: nowrap;

  &--claro {
    color: #fff;
  }
}

.logo__icono {
  width: 34px;
  height: 34px;
  flex-shrink: 0;
}

.logo__texto strong {
  color: $primary;
  font-weight: 700;

  .logo--claro & {
    color: #fde68a;
  }
}
</style>
