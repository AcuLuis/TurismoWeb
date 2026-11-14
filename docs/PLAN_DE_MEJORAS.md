# TurismoGo: plan de mejoras (lógica funcional y UX)

Documento de trabajo. Resume el diagnóstico del estado actual del proyecto y define, por fases, todo lo que se va a hacer para convertirlo en una aplicación usable por una empresa real: un marketplace donde empresas de turismo publican actividades y turistas las exploran, reservan y reseñan.

Fecha del diagnóstico: 14/11/2026. Rama: `luis_Acu`.

---

## 1. Cómo se hizo el diagnóstico

No fue solo lectura de código: el proyecto se ejecutó de verdad.

| Paso | Qué se hizo | Resultado |
|---|---|---|
| Backend | Compilación con .NET SDK 8.0.428 | Compila. 1 advertencia (cadena de conexión dentro del código). |
| Dependencias .NET | `dotnet list package --vulnerable --include-transitive` | 7 paquetes transitivos vulnerables (4 de severidad alta). |
| Dependencias npm | `npm audit --omit=dev` | 9 vulnerabilidades (2 críticas en Quasar 2.16.5). |
| Frontend | `quasar dev` + ESLint | 0 errores de lint. La app arranca. |
| Base de datos | SQL Server 2022 en un contenedor Docker temporal; esquema generado desde el modelo EF y datos de prueba | Funcionó, pero solo tras saltarse un bug de configuración (ver B-01). |
| API | Pruebas manuales de cada endpoint con `curl` | Se confirmaron fugas de contraseñas, errores 500 y endpoints de alta imposibles de usar. |
| UI | Capturas automatizadas (Playwright) de las 14 rutas, en escritorio (1366 px) y móvil (390 px), y pruebas de los flujos de login, registro y creación de actividad | Se confirmaron validaciones rotas, fechas desplazadas un día y pantallas vacías. |

---

## 2. Hallazgos principales (con evidencia)

### 2.1 Bloqueantes: hoy el sistema no puede funcionar fuera de la laptop original

| ID | Hallazgo | Evidencia |
|---|---|---|
| B-01 | `TurismoDbContext.OnConfiguring` fuerza `Server=LAPTOP-KSCIP1PS` sin comprobar `IsConfigured`, así que **ignora la cadena de `appsettings.json`**. | Al conectar a otro servidor se produce el error SQL 53: intenta ir a LAPTOP-KSCIP1PS. |
| B-02 | **No se pueden crear Usuarios, Reservas ni Reseñas.** Los controladores reciben la entidad EF completa y la validación exige las propiedades de navegación. | `POST /api/Usuario/Create` responde 400 con "The IdRolNavigation field is required." Lo mismo ocurre con Reserva y Reseña. |
| B-03 | No existe un script de base de datos que coincida con el código. El `TurismoGo.BD.sql` borrado describe otro modelo (`Usuarios` con `TipoUsuario`, `Estadisticas`, `Historial`…) y el modelo EF se generó desde una BD `turismo_db` que no está en el repositorio. | Comparación del SQL del commit `b53a13c` con `TurismoDbContext`. |
| B-04 | No hay autenticación. El login no consulta al backend y entra con campos vacíos. El rol lo elige el propio usuario con un radio button (incluido "Adm"). | Con login vacío se llega a `/#/principalU`. |
| B-05 | El registro no envía nada al backend. El botón tiene `@click="navigateTo('/')"`, que redirige antes de validar. | Con el formulario vacío, "Registrarse" vuelve al login como si el registro hubiera funcionado. |

### 2.2 Seguridad

| ID | Hallazgo | Evidencia |
|---|---|---|
| S-01 | **Las contraseñas se guardan y se devuelven en texto plano.** | `GET /api/Usuario/GetAll` y `GET /api/EmpresaTurismo/GetAll` devuelven `"contrasena":"secreta1"`. |
| S-02 | Todos los endpoints son públicos: cualquiera puede borrar usuarios, empresas o roles. | No hay `[Authorize]` en ningún controlador. |
| S-03 | Over-posting: `Update` recibe la entidad completa, así que el cliente puede cambiar `IdEmpresa`, `IdRol`, etc. | `ActividadController.UpdateActividad([FromBody] Actividad)`. |
| S-04 | Las excepciones salen como **500 con stack trace** (rutas del servidor, nombres de constraints). | `GET /api/Actividad/GetById?id=999` responde 500 con `System.Exception: Actividad not found` y la traza. |
| S-05 | CORS con `AllowAnyOrigin`, más un paquete `Microsoft.AspNetCore.Cors 2.2.0` obsoleto e innecesario. | `Program.cs`, los dos `.csproj`. |
| S-06 | Dependencias vulnerables: Quasar (2 críticas), postcss, source-map-js, System.Text.Json 8.0.0, System.Formats.Asn1, Azure.Identity y Microsoft.Extensions.Caching.Memory. | `npm audit` y `dotnet list package --vulnerable`. |
| S-07 | El perfil muestra el campo "Contraseña". Aunque salga como `********`, la pantalla da a entender que la contraseña se conserva legible. | `PerfilU.vue`, `PerfilE.vue`. |

### 2.3 Lógica de negocio

| ID | Hallazgo | Evidencia |
|---|---|---|
| L-01 | Se acepta una actividad con la **fecha de fin anterior a la de inicio y un precio negativo**. | `POST /api/Actividad/Create` con fin < inicio y `precio:-50` responde 201. Desde la UI, un precio de -20 muestra "Actividad creada exitosamente!". |
| L-02 | No hay control de cupos: la entidad `Actividad` perdió `Disponibilidad`, que sí existía en el SQL original. Riesgo de sobreventa. | `Actividad.cs`. |
| L-03 | **Falta el itinerario**, que el requisito RQ-005 exige. | `Actividad.cs` no tiene ese campo. |
| L-04 | La reserva no tiene reglas: `Estado` es texto libre, `FechaReserva` y `CantidadPersonas` vienen del cliente, no se calcula el total y se puede reservar una actividad pasada. | `Reserva.cs`, `ReservaController`. |
| L-05 | La reseña no tiene reglas: cualquiera puede reseñar cualquier actividad (sin haber reservado), varias veces y con `valoracion` fuera de 1 a 5. | `Reseña.cs`, sin índice único ni validación. |
| L-06 | No hay unicidad de correo, DNI ni RUC. | El esquema generado no tiene índices `UNIQUE`. |
| L-07 | Borrar un registro relacionado da 500 por FK (por ejemplo, un rol con usuarios), y no existe borrado lógico. | `DELETE /api/Rol/Delete/1` responde 500 con `DbUpdateException`. |
| L-08 | El modelo de cuentas está duplicado: `Usuario` y `EmpresaTurismo` tienen cada uno su correo y contraseña, más una tabla `Rol` aparte. Hay dos sistemas de login distintos. | Entidades. |
| L-09 | No hay búsqueda por destino ni por fechas en el backend (RQ-004). `GetAll` devuelve todo, sin paginar ni filtrar actividades pasadas. | `ActividadController.GetAllActividades`. |
| L-10 | El perfil de empresa en la UI muestra RUC, pero la entidad `EmpresaTurismo` no lo tiene. | `PerfilE.vue` frente a `EmpresaTurismo.cs`. |

### 2.4 UX / UI

| ID | Hallazgo | Evidencia |
|---|---|---|
| U-01 | **Las fechas se muestran un día antes.** `new Date("2026-12-01")` se interpreta en UTC y se muestra en hora de Perú (UTC-5). | La BD tiene 01/12/2026 y la tarjeta muestra 30/11/2026. |
| U-02 | Las reglas de validación del registro están rotas: `requiredRule()` devuelve un arreglo dentro de otro, y `passwordMatchRule` devuelve una función, así que siempre pasa. | Nombre vacío, correo inválido y contraseñas distintas no muestran ningún mensaje. |
| U-03 | El catálogo queda **en blanco** si la API falla o no hay datos: no hay estado de carga, ni de vacío, ni de error. | Captura de `/Visualiza_Actividad` con la API apagada. |
| U-04 | El sidebar muestra una identidad de relleno ("Razvan Stoenescu / @rstoenescu", el autor de Quasar) y un avatar de ejemplo. | `ELayaout.vue`, `ULayout.vue`. |
| U-05 | En móvil, el header se desborda: el título queda reducido a "T" y los 4 botones se amontonan. No hay menú hamburguesa. | Captura móvil de `principalU`. |
| U-06 | Las pantallas "Crear actividad" y "Catálogo" no tienen header ni navegación; no hay forma de volver salvo con el navegador. | Rutas sueltas fuera del layout. |
| U-07 | El formulario de actividad mezcla HTML nativo con Quasar, pide al usuario su **"ID Empresa"** y tiene dos campos redundantes ("Actividad" y "Nombre Actividad"). | Captura de `crearActividad`. |
| U-08 | Los errores 400 del backend no se muestran: el backend devuelve claves `NombreActividad` y el front espera `nombreActividad`. `errors.api` nunca se renderiza. | `Actividad_publica.vue`. |
| U-09 | Pantallas vacías o rotas: "Lista de actividades" muestra "tabla de actividades", "Lista de reservas" muestra "hola2", "Escribir reseña" lleva a un 404 y "Opciones" no hace nada. El panel de Admin enlaza a `/inicio`, `/actividades` y `/registrosl`, que no existen. | Capturas y `routes.js`. |
| U-10 | El precio se muestra en "$" en lugar de soles (S/) y sin formato. | Tarjeta: "$650", "$-50". |
| U-11 | En el diálogo "¿Cancelar registro?", el botón de confirmar dice **"Cerrar sesión"**. | `RegisterU.vue`, `RegisterE.vue`. |
| U-12 | Las tres páginas principales (Usuario, Empresa, Admin) son copias de unas 270 líneas cada una, con CSS duplicado y reglas `body {}` globales dentro de componentes. | `PrincipalU/E/A.vue`. |
| U-13 | Accesibilidad: `user-scalable=no, maximum-scale=1` impide hacer zoom (WCAG 1.4.4), no hay `lang="es"`, y los botones de icono no tienen texto accesible en móvil. | `index.html`. |
| U-14 | Las imágenes están enlazadas directamente desde sitios de terceros (civitatis, ohmachupicchu, huachos, travel1tours): riesgo de derechos de autor y de enlaces rotos. | `PrincipalU/E/A.vue`. |
| U-15 | Hay erratas y textos inconsistentes: "!Bienvenido" (debe ser "¡Bienvenido"), "Machu Piccchu", "Estas seguro" (debe ser "Estás seguro"), "-Henry Miller\"", "regresar" en minúscula, la etiqueta "dni", el título "TurismoGoApp" y el 404 en inglés ("Oops. Nothing here..."). | Varias vistas. |
| U-16 | El usuario tiene que registrarse antes de poder ver el catálogo, lo que reduce la conversión. | `routes.js`: `/` es el login. |

### 2.5 Mantenibilidad y repositorio

| ID | Hallazgo |
|---|---|
| M-01 | Sin `.gitignore` en la raíz: `bin/`, `obj/`, `.vs/` y `node_modules/` (unos 10 000 archivos) se subirían al repositorio. |
| M-02 | Anidación triple (`TurismoGo.Api/TurismoGO.API/TurismoGO.API`) y mayúsculas inconsistentes (`TurismoGO` frente a `TurismoGo`). |
| M-03 | Restos de plantilla: `WeatherForecast`, `example-store.js`, `IndexPage.vue`, `EssentialLink.vue`, `vue.config.js` (Quasar con Vite no lo usa) y `axios.js` con `baseURL: "https://api.example.com"`. |
| M-04 | La URL de la API (`http://localhost:5259`) está escrita a mano en cada componente; el puerto está fijado en `Program.cs`, que además usa `UseHttpsRedirection` sin HTTPS configurado. |
| M-05 | El proyecto `TurismoGo.Domain` contiene también la infraestructura (EF, repositorios); no hay capa de servicios, así que las reglas de negocio no tienen dónde vivir. |
| M-06 | No hay pruebas automatizadas, ni migraciones, ni datos semilla, ni instrucciones de ejecución. |
| M-07 | `package.json.engines` no admite Node 22 ni 24 (en esta máquina está Node 24). |

---

## 3. Plan por fases

Cada fase deja el proyecto funcionando y se cierra con uno o más commits en la rama `luis_Acu`. No se usará ninguna función de GitHub: ni Actions, ni Codespaces, ni Packages, ni nada de pago. Todo se ejecuta en local.

Tamaño estimado: **S** (menos de medio día), **M** (1 o 2 días), **L** (3 a 5 días).

### Fase 0: base del repositorio y entorno reproducible (S/M)

Objetivo: que cualquier persona clone el repositorio y lo levante en minutos.

1. `.gitignore` en la raíz (`bin/`, `obj/`, `.vs/`, `node_modules/`, `dist/`, `.quasar/`, `*.user`, `appsettings.*.local.json`, `.env.local`).
2. Reorganizar carpetas: `backend/` (solución .NET) y `frontend/` (Quasar), con nombres consistentes (`TurismoGo.*`).
3. Corregir B-01: eliminar `OnConfiguring` con el servidor fijo. La cadena se leerá de `appsettings.Development.json` o de `dotnet user-secrets`.
4. Añadir `docker-compose.yml` con SQL Server 2022 Developer para desarrollo local (gratuito), con el `sa` tomado de un `.env` no versionado. Dejar documentada la alternativa con SQL Server Express o LocalDB.
5. Sustituir los SQL sueltos por **migraciones de EF Core** y datos semilla de demo: un admin, una empresa verificada, un turista y unas 8 actividades reales de Perú. Pasar el contenido de `requisitos.txt` a `docs/requisitos.md`.
6. Actualizar dependencias: Quasar 2.x más reciente, `@quasar/app-vite`, postcss, EF Core 8.0.x más reciente, Swashbuckle. Quitar `Microsoft.AspNetCore.Cors 2.2.0`. Ampliar `engines` a Node 20, 22 y 24. Volver a pasar `npm audit` y `dotnet list package --vulnerable` hasta dejarlos en 0.
7. Borrar los restos de plantilla (M-03) y centralizar la URL de la API en una variable de entorno (`API_URL` en `quasar.config.js`) con una única instancia de axios.

**Terminado cuando:** `docker compose up -d`, `dotnet ef database update`, `dotnet run` y `npm run dev` funcionan en una máquina limpia siguiendo solo el README.

### Fase 1: seguridad y autenticación (L)

Objetivo: cerrar los riesgos B-04, B-05 y S-01 a S-07.

1. **Unificar el modelo de cuentas (L-08):** una tabla `Usuario` (correo único, hash de contraseña, rol `Turista`, `Empresa` o `Admin`, estado activo) y una tabla `EmpresaTurismo` como perfil 1:1 de un usuario con rol Empresa (RUC, razón social, teléfono, dirección, estado de verificación). Se elimina la tabla `Rol` editable; el rol pasa a ser un enum.
2. Contraseñas con hash mediante `PasswordHasher<T>` de ASP.NET Core Identity (PBKDF2, incluido en el framework y sin coste).
3. Autenticación **JWT**:
   - `POST /api/auth/login`
   - `POST /api/auth/registro/turista`
   - `POST /api/auth/registro/empresa`
   - `GET /api/auth/me`
4. Autorización por rol y por propiedad: una empresa solo edita sus actividades; un turista solo ve sus reservas; las acciones de administración, solo el Admin.
5. **DTOs de entrada y salida en todos los endpoints**: nunca se reciben ni se devuelven entidades EF. Esto también corrige B-02 y S-03.
6. Manejador global de excepciones con `ProblemDetails`: 404, 409 y 422 con mensajes en español, sin stack traces.
7. CORS restringido a orígenes configurados y *rate limiting* en el login (`Microsoft.AspNetCore.RateLimiting`, incluido en .NET 8).
8. Frontend:
   - store de Pinia `auth` e interceptor de axios (token y manejo de 401);
   - guards de ruta con `meta.requiresAuth` y `meta.roles`;
   - logout real que limpia la sesión;
   - el login detecta el rol desde el backend, así que desaparece el radio "Usuario/Empresa/Adm".

**Terminado cuando:** sin token, toda ruta protegida devuelve 401; ninguna respuesta contiene contraseñas; un turista no puede editar actividades ajenas (verificado con pruebas).

### Fase 2: lógica de dominio y API (L)

Objetivo: que las reglas del negocio vivan en el backend y no dependan del cliente.

1. Arquitectura: separar `TurismoGo.Domain` (entidades y reglas), `TurismoGo.Application` (servicios, DTOs y validadores) y `TurismoGo.Infrastructure` (EF y repositorios). Los controladores quedan delgados.
2. Validación con FluentValidation o DataAnnotations:
   - fechas: fin ≥ inicio e inicio ≥ hoy;
   - precio > 0;
   - DNI de 8 dígitos y RUC de 11 dígitos (con prefijo 10 o 20);
   - teléfono peruano;
   - longitudes máximas alineadas con la BD.
3. Modelo de **Actividad** ampliado:
   - itinerario por días (RQ-005);
   - capacidad (cupos);
   - duración;
   - punto de encuentro;
   - qué incluye y qué no incluye;
   - imagen de portada;
   - estado (`Borrador`, `Publicada`, `Cancelada`, `Finalizada`).
4. **Reservas** con reglas:
   - estados `Pendiente`, `Confirmada`, `Cancelada` y `Completada`;
   - la fecha y el total (precio × personas) los fija el servidor, y el total se guarda como foto del precio de ese momento;
   - validación de cupos dentro de una transacción con control de concurrencia (`rowversion`) para evitar sobreventa;
   - no se puede reservar una actividad pasada ni cancelada;
   - política de cancelación configurable (por ejemplo, hasta 48 h antes).
5. **Reseñas** con reglas: solo con una reserva completada, una por usuario y actividad (índice único), valoración de 1 a 5 y fecha del servidor. El promedio y el conteo se exponen en el listado.
6. Índices únicos para correo, RUC y DNI. Borrado lógico (`Activo`) y respuesta 409 en lugar de 500 cuando hay dependencias. Campos de auditoría (`CreadoEn`, `ActualizadoEn`).
7. API REST coherente y paginada:
   - `GET /api/actividades?q=&destino=&desde=&hasta=&precioMin=&precioMax=&orden=&pagina=&tamano=` (RQ-004): por defecto solo actividades publicadas y futuras;
   - `GET /api/actividades/{id}`: detalle con empresa, itinerario, promedio y reseñas (RQ-005);
   - `GET /api/destinos`: lista de destinos para el filtro;
   - recursos anidados y propios: `/api/mis/reservas`, `/api/empresa/actividades`, `/api/empresa/reservas`.
8. Fechas: `DateOnly` en toda la API (sin zonas horarias) y moneda fija en PEN.

**Terminado cuando:** las reglas de cupos, fechas y reseñas tienen pruebas unitarias en verde, y Swagger documenta todos los endpoints con ejemplos.

### Fase 3: UX base, diseño y navegación (M)

Objetivo: una aplicación coherente, accesible y usable en móvil.

1. **Un solo layout** (`AppLayout`) con header responsive y drawer. El menú se genera según el rol, con hamburguesa en móvil. Sustituye a las 3 páginas `Principal*` duplicadas, a `ELayaout`/`ULayout` y a las rutas sueltas (U-05, U-06, U-12).
2. **Landing pública con el catálogo visible sin login** (U-16). El login solo se pide al reservar y luego se vuelve a la actividad (`?redirect=`).
3. Identidad visual:
   - paleta de marca en `quasar.variables.scss` y tipografía única;
   - imágenes propias o con licencia libre en `public/` (U-14);
   - favicon y logo propios;
   - pack de idioma `es` de Quasar.
4. Plugins de Quasar `Notify`, `Loading` y `Dialog` para dar feedback homogéneo (éxito, error, confirmaciones).
5. Componentes de estado reutilizables: *skeleton* de carga, estado vacío con una acción sugerida y estado de error con "Reintentar" (U-03).
6. Utilidades compartidas:
   - `formatFecha` sin el desfase UTC (U-01);
   - `formatMoneda` con `Intl.NumberFormat('es-PE', { currency: 'PEN' })`, que muestra "S/ 650.00" (U-10);
   - reglas de validación reutilizables y correctas (U-02).
7. Accesibilidad:
   - permitir zoom y `lang="es"`;
   - `aria-label` en botones de icono;
   - foco visible y contraste AA (U-13).
8. Corregir textos y erratas (U-11, U-15) y traducir el 404 al español, con un botón que lleve al inicio según el rol.
9. Quitar la identidad de relleno del sidebar y mostrar el nombre real del usuario con un avatar de iniciales (U-04).

**Terminado cuando:** las 14 rutas actuales se ven correctas a 390 px y 1366 px, sin desbordes, y no queda ninguna pantalla en blanco ni con texto de relleno.

### Fase 4: flujo del turista (RQ-004 y RQ-005) (L)

1. **Catálogo:**
   - filtros por destino (select), rango de fechas, precio y orden (precio, fecha, mejor valorados);
   - chips con los filtros activos y botón "Limpiar";
   - filtros reflejados en la URL, para poder compartirlos y conservarlos al volver;
   - paginación.
2. **Tarjeta de actividad:**
   - imagen, nombre, empresa, destino y fechas correctas;
   - duración, precio en S/ y promedio de estrellas con número de reseñas;
   - aviso de "Quedan N cupos" cuando quedan pocos.
3. **Detalle de actividad (`/actividades/:id`):**
   - portada, descripción, itinerario por días (timeline), qué incluye y punto de encuentro;
   - datos de la empresa;
   - reseñas con promedio y distribución;
   - panel fijo de reserva.
4. **Flujo de reserva:**
   - selector de personas limitado por los cupos y total calculado en vivo;
   - diálogo de confirmación con el resumen;
   - pantalla de éxito con el código de reserva.
   - No incluye pago en línea: queda fuera de alcance y se indica como "pago coordinado con la empresa".
5. **Mis reservas:**
   - pestañas Próximas, Pasadas y Canceladas;
   - cancelar respetando la política, con explicación clara;
   - "Escribir reseña" solo en las reservas completadas (corrige el enlace roto de U-09).
6. **Perfil:** datos reales, edición en diálogo y "Cambiar contraseña" como acción aparte (nunca se muestra la contraseña).

### Fase 5: flujo de la empresa (RQ-003) (L)

1. **Panel de inicio** con indicadores (actividades publicadas, reservas del mes, ingresos estimados, valoración media) y próximas salidas.
2. **Mis actividades:** tabla `q-table` con búsqueda, chips de estado y acciones (editar, duplicar, publicar o despublicar, cancelar).
3. **Formulario de actividad** rehecho con componentes Quasar:
   - la empresa se toma del token (se elimina "ID Empresa") y se quita el campo duplicado;
   - selector de rango de fechas con mínimo en hoy y precio con prefijo S/;
   - cupos;
   - editor de itinerario por días;
   - imagen de portada (URL en la primera versión);
   - vista previa de la tarjeta antes de publicar y guardado como borrador;
   - errores del backend mostrados junto a cada campo (U-07, U-08).
4. **Reservas recibidas** (sustituye a "hola2"): lista por actividad y fecha, confirmar o rechazar, lista de participantes y **exportación a CSV**, útil como manifiesto de pasajeros.
5. **Perfil de empresa:** RUC (L-10), razón social, contacto, descripción y logo; edición. Aviso visible mientras la empresa no esté verificada.

### Fase 6: administración (M)

1. Panel real de administración (hoy es una copia del de empresa, con enlaces a rutas inexistentes).
2. **Verificación de empresas:** estados `Pendiente`, `Verificada` y `Suspendida`. Solo las empresas verificadas pueden publicar, lo que da confianza al marketplace.
3. Gestión de usuarios (buscar, suspender o reactivar) y moderación de reseñas (ocultar con motivo).
4. Reportes básicos: reservas por mes, destinos más reservados y empresas con mejor valoración.

### Fase 7: calidad, pruebas y operación (M)

1. Backend: **xUnit**.
   - Pruebas unitarias de las reglas: cupos, fechas, elegibilidad de reseña y cancelación.
   - Pruebas de integración con `WebApplicationFactory` sobre SQLite en memoria: auth, permisos y códigos HTTP.
2. Frontend: **Vitest** para utilidades y stores. Opcionalmente, Playwright para los flujos de login, reserva y creación de actividad, que se ejecutan en local.
3. Swagger con soporte para JWT y comentarios XML; endpoint `/health`; logs estructurados con `ILogger`.
4. Scripts locales para verificarlo todo con un comando (`npm run lint`, `npm run test`, `dotnet test`). Sin CI de GitHub; como mucho, un *hook* de git local opcional.

### Fase 8: README y licencia (S)

1. Archivo `LICENSE` con la licencia **MIT**.
2. `README.md` en la raíz con:
   - descripción y propósito;
   - capturas de pantalla;
   - stack y arquitectura;
   - requisitos previos;
   - cómo ejecutarlo paso a paso;
   - usuarios de demo;
   - estructura de carpetas;
   - resumen de endpoints;
   - hoja de ruta;
   - licencia MIT;
   - una sección final invitando a dar una estrella al repositorio si el proyecto gustó.
3. Regla de estilo: **el único emoji o sticker de todo el README será la estrella de esa sección final.** El resto del README no lleva emojis, stickers ni badges decorativos.

---

## 4. Orden recomendado y dependencias

```
Fase 0 ──> Fase 1 ──> Fase 2 ──┬──> Fase 4 (turista)
                               ├──> Fase 5 (empresa)
                               └──> Fase 6 (admin)
Fase 3 (UX base) puede avanzar en paralelo desde la Fase 1.
Fase 7 acompaña a todas (cada regla nueva lleva su prueba) y se cierra al final.
Fase 8 al final, con capturas de la versión terminada.
```

Las fases 0, 1 y 2 son imprescindibles: sin ellas el sistema no es seguro ni puede guardar registros. Las fases 3 a 5 son las que convierten el proyecto en un producto presentable. La fase 6 es la que lo hace creíble para una empresa.

## 5. Fuera de alcance (a propósito)

- Pagos en línea (requiere una pasarela y cumplimiento PCI).
- Envío real de correos: en la primera versión se registra en el log; se podrá integrar un SMTP después.
- Subida de imágenes a la nube: en la primera versión se usan URL; se puede pasar a almacenamiento local más adelante.
- Aplicaciones móviles nativas: la web será responsive.
