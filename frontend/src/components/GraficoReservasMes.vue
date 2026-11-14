<template>
  <figure class="grafico" :aria-labelledby="idTitulo">
    <div class="row items-center justify-between q-mb-sm">
      <h2 :id="idTitulo" class="text-h6 q-my-none">{{ titulo }}</h2>
      <q-btn-toggle
        v-model="vista"
        dense
        no-caps
        unelevated
        toggle-color="primary"
        color="grey-2"
        text-color="grey-9"
        :options="[
          { label: 'Gráfico', value: 'grafico' },
          { label: 'Tabla', value: 'tabla' },
        ]"
        aria-label="Cambiar entre gráfico y tabla"
      />
    </div>

    <!-- Vista de gráfico: una sola serie, sin leyenda (el título la nombra) -->
    <div v-if="vista === 'grafico'" class="grafico__area" role="img" :aria-label="resumenAccesible">
      <div class="grafico__eje-y" aria-hidden="true">
        <span v-for="t in marcas" :key="t" :style="{ bottom: `${(t / maximo) * 100}%` }">{{ t }}</span>
      </div>
      <div class="grafico__plot">
        <div v-for="t in marcas" :key="`g${t}`" class="grafico__guia" :style="{ bottom: `${(t / maximo) * 100}%` }" aria-hidden="true" />
        <div
          v-for="(m, i) in datos"
          :key="m.mes"
          class="grafico__columna"
          tabindex="0"
          :aria-label="`${nombreMes(m.mes, true)}: ${m.reservas} reservas, ${formatMoneda(m.montoConfirmado)} confirmados`"
        >
          <span v-if="i === datos.length - 1 && m.reservas > 0" class="grafico__etiqueta" :style="{ bottom: `calc(${(m.reservas / maximo) * 100}% + 4px)` }">
            {{ m.reservas }}
          </span>
          <div v-if="m.reservas > 0" class="grafico__barra" :style="{ height: `${(m.reservas / maximo) * 100}%` }" />
          <q-tooltip anchor="top middle" self="bottom middle" :offset="[0, 6]">
            <div class="text-weight-medium">{{ nombreMes(m.mes, true) }}</div>
            <div>{{ pluralizar(m.reservas, 'reserva') }}</div>
            <div>{{ formatMoneda(m.montoConfirmado) }} confirmados</div>
          </q-tooltip>
        </div>
      </div>
      <div class="grafico__eje-x" aria-hidden="true">
        <span v-for="m in datos" :key="`x${m.mes}`">{{ nombreMes(m.mes) }}</span>
      </div>
    </div>

    <!-- Vista de tabla (accesible y exportable) -->
    <q-markup-table v-else flat dense separator="horizontal">
      <thead>
        <tr><th class="text-left">Mes</th><th class="text-right">Reservas</th><th class="text-right">Monto confirmado</th></tr>
      </thead>
      <tbody>
        <tr v-for="m in datos" :key="m.mes">
          <td>{{ nombreMes(m.mes, true) }}</td>
          <td class="text-right">{{ m.reservas }}</td>
          <td class="text-right">{{ formatMoneda(m.montoConfirmado) }}</td>
        </tr>
      </tbody>
    </q-markup-table>
  </figure>
</template>

<script setup>
import { computed, ref, useId } from 'vue'
import { formatMoneda, pluralizar } from '@/utils/formato'

const props = defineProps({
  titulo: { type: String, default: 'Reservas por mes' },
  /** [{ mes: 'YYYY-MM', reservas, montoConfirmado }] */
  datos: { type: Array, required: true },
})

const vista = ref('grafico')
const idTitulo = `grafico-${useId()}`

/** Escala "redonda" para el eje Y. */
const maximo = computed(() => {
  const mayor = Math.max(1, ...props.datos.map((d) => d.reservas))
  const paso = mayor <= 5 ? 1 : mayor <= 20 ? 5 : mayor <= 50 ? 10 : 10 ** Math.floor(Math.log10(mayor))
  return Math.ceil(mayor / paso) * paso
})
const marcas = computed(() => [0, maximo.value / 2, maximo.value].filter((v, i, a) => a.indexOf(v) === i && Number.isInteger(v)))

function nombreMes(clave, largo = false) {
  const [a, m] = clave.split('-').map(Number)
  const texto = new Intl.DateTimeFormat('es-PE', largo ? { month: 'long', year: 'numeric' } : { month: 'short' }).format(new Date(a, m - 1, 1))
  return texto.replace('.', '').replace(/^\w/, (c) => c.toUpperCase())
}

const resumenAccesible = computed(
  () => `${props.titulo}. ` + props.datos.map((d) => `${nombreMes(d.mes, true)}: ${d.reservas}`).join('; '),
)
</script>

<style scoped>
.grafico {
  margin: 0;
}

.grafico__area {
  display: grid;
  grid-template-columns: 28px 1fr;
  grid-template-rows: 180px auto;
  column-gap: 8px;
}

.grafico__eje-y {
  position: relative;
}

.grafico__eje-y span {
  position: absolute;
  right: 0;
  transform: translateY(50%);
  font-size: 0.72rem;
  color: #64748b;
}

.grafico__plot {
  position: relative;
  display: flex;
  align-items: flex-end;
  justify-content: space-around;
  border-bottom: 1px solid #cbd5e1;
}

.grafico__guia {
  position: absolute;
  left: 0;
  right: 0;
  border-top: 1px dashed #e2e8f0;
}

.grafico__columna {
  position: relative;
  flex: 1;
  height: 100%;
  display: flex;
  align-items: flex-end;
  justify-content: center;
  cursor: default;
  border-radius: 6px;
}

.grafico__columna:hover .grafico__barra,
.grafico__columna:focus-visible .grafico__barra {
  background: #0f766e;
}

.grafico__barra {
  width: 24px;
  min-height: 3px;
  background: #0d9488;
  border-radius: 4px 4px 0 0;
  transition: background 0.15s ease;
}

.grafico__etiqueta {
  position: absolute;
  font-size: 0.8rem;
  font-weight: 600;
  color: #0f172a;
}

.grafico__eje-x {
  grid-column: 2;
  display: flex;
  justify-content: space-around;
  padding-top: 6px;
}

.grafico__eje-x span {
  flex: 1;
  text-align: center;
  font-size: 0.75rem;
  color: #64748b;
}
</style>
