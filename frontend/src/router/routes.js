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

      // Empresa
      { path: 'empresa', name: 'empresa-panel', component: () => import('@/pages/empresa/PanelEmpresaPage.vue'), meta: { titulo: 'Panel de empresa', requiereSesion: true, roles: ['Empresa'] } },
      { path: 'empresa/actividades', name: 'empresa-actividades', component: () => import('@/pages/empresa/ActividadesEmpresaPage.vue'), meta: { titulo: 'Mis actividades', requiereSesion: true, roles: ['Empresa'] } },
      { path: 'empresa/actividades/nueva', name: 'empresa-actividad-nueva', component: () => import('@/pages/empresa/ActividadFormPage.vue'), meta: { titulo: 'Nueva actividad', requiereSesion: true, roles: ['Empresa'] } },
      { path: 'empresa/actividades/:id(\\d+)/editar', name: 'empresa-actividad-editar', component: () => import('@/pages/empresa/ActividadFormPage.vue'), meta: { titulo: 'Editar actividad', requiereSesion: true, roles: ['Empresa'] } },
      { path: 'empresa/reservas', name: 'empresa-reservas', component: () => import('@/pages/empresa/ReservasRecibidasPage.vue'), meta: { titulo: 'Reservas recibidas', requiereSesion: true, roles: ['Empresa'] } },
      { path: 'empresa/perfil', name: 'empresa-perfil', component: () => import('@/pages/empresa/PerfilEmpresaPage.vue'), meta: { titulo: 'Perfil de empresa', requiereSesion: true, roles: ['Empresa'] } },

      // Administración
      { path: 'admin', name: 'admin-panel', component: () => import('@/pages/admin/PanelAdminPage.vue'), meta: { titulo: 'Administración', requiereSesion: true, roles: ['Admin'] } },
      { path: 'admin/empresas', name: 'admin-empresas', component: () => import('@/pages/admin/EmpresasAdminPage.vue'), meta: { titulo: 'Empresas', requiereSesion: true, roles: ['Admin'] } },
      { path: 'admin/usuarios', name: 'admin-usuarios', component: () => import('@/pages/admin/UsuariosAdminPage.vue'), meta: { titulo: 'Usuarios', requiereSesion: true, roles: ['Admin'] } },
      { path: 'admin/resenas', name: 'admin-resenas', component: () => import('@/pages/admin/ResenasAdminPage.vue'), meta: { titulo: 'Moderación de reseñas', requiereSesion: true, roles: ['Admin'] } },
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
