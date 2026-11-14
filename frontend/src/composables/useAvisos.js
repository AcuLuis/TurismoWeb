import { useQuasar } from 'quasar'
import { mensajeError } from '@/utils/errores'

/** Notificaciones homogéneas para toda la aplicación. */
export function useAvisos() {
  const $q = useQuasar()

  return {
    exito: (mensaje) => $q.notify({ type: 'positive', message: mensaje, icon: 'check_circle' }),
    info: (mensaje) => $q.notify({ type: 'info', message: mensaje, icon: 'info' }),
    error: (error, porDefecto) =>
      $q.notify({ type: 'negative', message: mensajeError(error, porDefecto), icon: 'error', timeout: 5000 }),

    /** Diálogo de confirmación. Devuelve una promesa que se resuelve en true/false. */
    confirmar: ({ titulo, mensaje, textoAceptar = 'Aceptar', textoCancelar = 'Cancelar', peligro = false }) =>
      new Promise((resolve) => {
        $q.dialog({
          title: titulo,
          message: mensaje,
          persistent: true,
          ok: { label: textoAceptar, color: peligro ? 'negative' : 'primary', unelevated: true, noCaps: true },
          cancel: { label: textoCancelar, flat: true, color: 'grey-8', noCaps: true },
        })
          .onOk(() => resolve(true))
          .onCancel(() => resolve(false))
          .onDismiss(() => resolve(false))
      }),

    /** Diálogo que pide un motivo obligatorio. Devuelve el texto o null si se cancela. */
    pedirMotivo: ({ titulo, mensaje, textoAceptar = 'Confirmar', peligro = true, min = 5, max = 300 }) =>
      new Promise((resolve) => {
        $q.dialog({
          title: titulo,
          message: mensaje,
          prompt: {
            model: '',
            type: 'textarea',
            outlined: true,
            autogrow: true,
            counter: true,
            maxlength: max,
            isValid: (v) => v.trim().length >= min,
            label: `Motivo (mínimo ${min} caracteres)`,
          },
          persistent: true,
          ok: { label: textoAceptar, color: peligro ? 'negative' : 'primary', unelevated: true, noCaps: true },
          cancel: { label: 'Volver', flat: true, color: 'grey-8', noCaps: true },
        })
          .onOk((texto) => resolve(texto.trim()))
          .onCancel(() => resolve(null))
          .onDismiss(() => resolve(null))
      }),
  }
}
