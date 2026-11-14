// Rutas de la aplicación.
// meta.requiereSesion: exige sesión iniciada. meta.roles: roles permitidos. meta.soloInvitados: solo sin sesión.
// meta.titulo: título de la pestaña del navegador.

const routes = [
  {
    path: '/',
    component: () => import('@/layouts/AppLayout.vue'),
    children: [
      // Público
      { path: '', name: 'inicio', component: () => import('@/pages/publico/InicioPage.vue'), meta: { titulo: 'Actividades turísticas en Perú' } },
      { path: 'actividades', name: 'catalogo', component: () => import('@/pages/publico/CatalogoPage.vue'), meta: { titulo: 'Explorar actividades' } },
      { path: 'actividades/:id(\\d+)', name: 'actividad', component: () => import('@/pages/publico/ActividadDetallePage.vue'), meta: { titulo: 'Actividad' } },

      // Cuenta (cualquier rol)
      { path: 'cuenta', name: 'cuenta', component: () => import('@/pages/cuenta/CuentaPage.vue'), meta: { titulo: 'Mi cuenta', requiereSesion: true } },

      // Turista
      { path: 'mis-reservas', name: 'mis-reservas', component: () => import('@/pages/turista/MisReservasPage.vue'), meta: { titulo: 'Mis reservas', requiereSesion: true, roles: ['Turista'] } },
    ],
  },
  {
    path: '/',
    component: () => import('@/layouts/AuthLayout.vue'),
    children: [
      { path: 'ingresar', name: 'ingresar', component: () => import('@/pages/auth/LoginPage.vue'), meta: { titulo: 'Ingresar', soloInvitados: true } },
      { path: 'registro', name: 'registro', component: () => import('@/pages/auth/RegistroPage.vue'), meta: { titulo: 'Crear cuenta', soloInvitados: true } },
    ],
  },
  {
    path: '/:catchAll(.*)*',
    component: () => import('@/pages/ErrorNotFound.vue'),
    meta: { titulo: 'Página no encontrada' },
  },
]

export default routes
