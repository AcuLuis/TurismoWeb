using TurismoGo.Application.Abstracciones;
using TurismoGo.Domain;
using TurismoGo.Domain.Entidades;

namespace TurismoGo.Infrastructure.Datos;

/// <summary>
/// Datos de demostración (solo desarrollo). Las fechas son relativas al día actual para que el catálogo
/// siempre tenga salidas próximas. Contraseña de todas las cuentas demo: <see cref="PasswordDemo"/>.
/// </summary>
internal sealed class DatosDemo(TurismoDbContext db, IHasherContrasenas hasher, IReloj reloj)
{
    public const string PasswordDemo = "Demo2026!";

    public async Task SembrarAsync(CancellationToken ct)
    {
        var hoy = reloj.Hoy;
        var hash = hasher.Hash(PasswordDemo);

        // --- Empresas ---
        var andes = CrearEmpresa("Andes Explorer", "Andes Explorer Tours S.A.C.", "20601234567", "ventas@andesexplorer.pe",
            "Rosa", "Huamán Ccori", "984112233", "Calle Plateros 345, Cusco",
            "Operador cusqueño con 12 años de experiencia en trekking y turismo cultural en los Andes del sur.", hash);
        var costa = CrearEmpresa("Costa Viva", "Costa Viva Expediciones E.I.R.L.", "20509876543", "reservas@costaviva.pe",
            "Jorge", "Salazar Ríos", "956778899", "Av. Paracas 120, Pisco, Ica",
            "Aventura y naturaleza en la costa peruana: desierto, islas y gastronomía.", hash);
        var amazonia = CrearEmpresa("Amazonía Viva", "Amazonía Viva Turismo S.A.C.", "20611122233", "contacto@amazoniaviva.pe",
            "Milagros", "Panduro Vela", "965443322", "Malecón Tarapacá 210, Iquitos",
            "Expediciones a la selva baja peruana.", hash);
        andes.Empresa!.Verificar(reloj.UtcAhora);
        costa.Empresa!.Verificar(reloj.UtcAhora);
        // Amazonía Viva queda Pendiente: sirve para probar la verificación desde el panel de administración.

        // --- Turistas ---
        var ana = CrearTurista("Ana", "Quispe Mamani", "45678912", "ana.quispe@correo.pe", "987654321", hash);
        var carlos = CrearTurista("Carlos", "Mendoza Paredes", "41237895", "carlos.mendoza@correo.pe", "912345678", hash);
        var lucia = CrearTurista("Lucía", "Torres Vargas", "47851236", "lucia.torres@correo.pe", null, hash);

        db.Usuarios.AddRange(andes, costa, amazonia, ana, carlos, lucia);

        // --- Actividades próximas ---
        var caminoInca = CrearActividad(andes.Empresa, "Camino Inca clásico 4D/3N", "Cusco", hoy.AddDays(21), 4, 2450m, 16,
            "Recorre el legendario Camino Inca hasta Machu Picchu, atravesando sitios arqueológicos, bosque nuboso y el paso de Warmiwañusca. Grupos reducidos con guía bilingüe certificado.",
            "Hotel en el centro histórico de Cusco (recojo 4:30 a. m.)",
            "Permisos de ingreso, guía, cocinero, porteadores, carpas, alimentación completa, bus de bajada y tren de retorno.",
            "Bolsa de dormir, propinas, ingreso a Huayna Picchu.",
            ("Cusco – Wayllabamba", "Traslado al km 82 e inicio de la caminata por el valle del Urubamba hasta Wayllabamba (12 km)."),
            ("Paso de Warmiwañusca", "Ascenso al punto más alto de la ruta (4200 m s. n. m.) y descenso a Pacaymayo."),
            ("Sitios arqueológicos", "Visita a Runkurakay, Sayacmarca y Phuyupatamarca. Noche en Wiñay Wayna."),
            ("Machu Picchu", "Llegada por la Puerta del Sol al amanecer, visita guiada y retorno en tren a Cusco."));
        var machuPicchu = CrearActividad(andes.Empresa, "Machu Picchu full day en tren", "Cusco", hoy.AddDays(9), 1, 890m, 20,
            "Excursión de un día a la ciudadela de Machu Picchu en tren panorámico desde Ollantaytambo, con visita guiada de dos horas.",
            "Estación de Ollantaytambo (5:45 a. m.)",
            "Tren ida y vuelta, bus Aguas Calientes – Machu Picchu, ingreso, guía.",
            "Almuerzo, traslado Cusco – Ollantaytambo.",
            ("Machu Picchu", "Tren a Aguas Calientes, subida en bus, visita guiada y tiempo libre. Retorno por la tarde."));
        var vinicunca = CrearActividad(andes.Empresa, "Montaña de Colores (Vinicunca)", "Cusco", hoy.AddDays(5), 1, 120m, 25,
            "Caminata a la famosa Montaña de Siete Colores, a 5036 m s. n. m., con vistas al nevado Ausangate.",
            "Plaza San Francisco, Cusco (4:00 a. m.)",
            "Transporte, desayuno y almuerzo buffet, guía, botiquín y oxígeno.",
            "Ingreso a la montaña (S/ 25), caballo de alquiler.",
            ("Vinicunca", "Viaje a Quesiuno, caminata de 2 h hasta el mirador, descenso y almuerzo en Cusipata."));
        var titicaca = CrearActividad(andes.Empresa, "Lago Titicaca: Uros y Taquile 2D/1N", "Puno", hoy.AddDays(14), 2, 340m, 18,
            "Navega el lago navegable más alto del mundo, visita las islas flotantes de los Uros y convive con una familia en Amantaní.",
            "Puerto de Puno (7:00 a. m.)",
            "Lancha, guía, alojamiento rural, tres comidas.",
            "Traslado hotel – puerto.",
            ("Uros y Amantaní", "Islas flotantes de los Uros, navegación a Amantaní y noche con familia anfitriona."),
            ("Taquile", "Caminata por la isla de Taquile, almuerzo y retorno a Puno."));
        var colca = CrearActividad(andes.Empresa, "Cañón del Colca 2D/1N", "Arequipa", hoy.AddDays(18), 2, 420m, 20,
            "Uno de los cañones más profundos del mundo: miradores, baños termales y el vuelo del cóndor.",
            "Plaza de Armas de Arequipa (8:00 a. m.)",
            "Transporte turístico, hotel en Chivay, desayuno, guía.",
            "Boleto turístico del Colca, almuerzos.",
            ("Arequipa – Chivay", "Reserva de Salinas y Aguada Blanca, Patapampa y baños termales de La Calera."),
            ("Cruz del Cóndor", "Avistamiento de cóndores, pueblos de Maca y Yanque, retorno a Arequipa."));
        var ballestas = CrearActividad(costa.Empresa, "Islas Ballestas y Reserva de Paracas", "Ica", hoy.AddDays(7), 1, 180m, 30,
            "Paseo en lancha a las Islas Ballestas para ver lobos marinos y pingüinos de Humboldt, y recorrido por la Reserva Nacional de Paracas.",
            "Muelle El Chaco, Paracas (8:00 a. m.)",
            "Lancha, chalecos, guía, bus en la reserva.",
            "Tasa de embarque e ingreso a la reserva.",
            ("Paracas", "Islas Ballestas por la mañana; tarde en la Reserva de Paracas con la playa Roja y el mirador de La Catedral."));
        var huacachina = CrearActividad(costa.Empresa, "Huacachina: tubulares y sandboard", "Ica", hoy.AddDays(8), 1, 95m, 24,
            "Recorrido en buggies por las dunas que rodean el oasis de Huacachina, con práctica de sandboard y atardecer en el desierto.",
            "Laguna de Huacachina (3:30 p. m.)",
            "Tubular, tabla de sandboard, conductor experimentado.",
            "Tasa municipal (S/ 3.50).",
            ("Desierto de Ica", "Dos horas de dunas, sandboard en tres pendientes y atardecer."));
        var lima = CrearActividad(costa.Empresa, "Lima colonial y gastronómica", "Lima", hoy.AddDays(3), 1, 150m, 15,
            "Centro histórico de Lima, catacumbas de San Francisco y una ruta de degustación por el Barrio Chino y el Mercado Central.",
            "Plaza Mayor de Lima (9:30 a. m.)",
            "Guía, degustaciones, ingreso a San Francisco.",
            "Traslados desde el hotel.",
            ("Centro de Lima", "Plaza Mayor, Catedral, San Francisco y ruta gastronómica."));
        var laguna69 = CrearActividad(costa.Empresa, "Trekking Laguna 69", "Áncash", hoy.AddDays(25), 1, 160m, 14,
            "Una de las caminatas más bellas de la Cordillera Blanca, hasta la laguna turquesa al pie del nevado Chacraraju.",
            "Plaza de Armas de Huaraz (5:00 a. m.)",
            "Transporte, guía de montaña, desayuno.",
            "Ingreso al Parque Nacional Huascarán, almuerzo.",
            ("Laguna 69", "Viaje a Cebollapampa, ascenso de 3 h a la laguna, descenso y retorno a Huaraz."));

        foreach (var a in new[] { caminoInca, machuPicchu, vinicunca, titicaca, colca, ballestas, huacachina, lima, laguna69 })
            a.Estado = EstadoActividad.Publicada;

        var kayak = CrearActividad(costa.Empresa, "Kayak en la bahía de Paracas", "Ica", hoy.AddDays(30), 1, 130m, 10,
            "Recorrido en kayak por la bahía de Paracas con avistamiento de aves.", "Playa El Chaco",
            null, null, ("Bahía de Paracas", "Kayak guiado de 2 horas."));
        var iquitos = CrearActividad(amazonia.Empresa, "Expedición Pacaya Samiria 4D/3N", "Loreto", hoy.AddDays(40), 4, 1650m, 12,
            "Navegación por la reserva Pacaya Samiria con avistamiento de delfines rosados.", "Puerto de Nauta",
            null, null, ("Nauta", "Navegación al río Marañón."));

        // --- Salidas ya realizadas (para reseñas) ---
        var vinicuncaPasada = CrearActividad(andes.Empresa, "Montaña de Colores (Vinicunca)", "Cusco", hoy.AddDays(-20), 1, 110m, 25,
            vinicunca.Descripcion, vinicunca.PuntoEncuentro, vinicunca.Incluye, vinicunca.NoIncluye,
            ("Vinicunca", "Caminata al mirador de la Montaña de Colores."));
        var ballestasPasada = CrearActividad(costa.Empresa, "Islas Ballestas y Reserva de Paracas", "Ica", hoy.AddDays(-12), 1, 170m, 30,
            ballestas.Descripcion, ballestas.PuntoEncuentro, ballestas.Incluye, ballestas.NoIncluye,
            ("Paracas", "Islas Ballestas y Reserva de Paracas."));
        vinicuncaPasada.Estado = EstadoActividad.Publicada;
        ballestasPasada.Estado = EstadoActividad.Publicada;

        db.Actividades.AddRange(caminoInca, machuPicchu, vinicunca, titicaca, colca, ballestas, huacachina, lima, laguna69,
            kayak, iquitos, vinicuncaPasada, ballestasPasada);

        // --- Reservas ---
        Reservar(ana, caminoInca, 2, EstadoReserva.Confirmada);
        Reservar(ana, ballestas, 2, EstadoReserva.Pendiente);
        Reservar(carlos, machuPicchu, 3, EstadoReserva.Pendiente);
        Reservar(carlos, vinicunca, 1, EstadoReserva.Confirmada);
        Reservar(lucia, titicaca, 2, EstadoReserva.Confirmada);
        Reservar(lucia, lima, 1, EstadoReserva.Pendiente);
        var cancelada = Reservar(carlos, colca, 2, EstadoReserva.Confirmada);
        cancelada.Cancelar(reloj.UtcAhora, "Cambio de planes del cliente.");

        Reservar(ana, vinicuncaPasada, 2, EstadoReserva.Confirmada);
        Reservar(carlos, vinicuncaPasada, 1, EstadoReserva.Confirmada);
        Reservar(lucia, vinicuncaPasada, 1, EstadoReserva.Confirmada);
        Reservar(ana, ballestasPasada, 2, EstadoReserva.Confirmada);
        Reservar(lucia, ballestasPasada, 3, EstadoReserva.Confirmada);

        // --- Reseñas ---
        db.Resenas.AddRange(
            CrearResena(ana, vinicuncaPasada, 5, "Paisaje increíble y el guía estuvo pendiente de todo el grupo con la altura. Muy recomendado."),
            CrearResena(carlos, vinicuncaPasada, 4, "Muy buena organización. La caminata es exigente, lleven abrigo y bloqueador."),
            CrearResena(lucia, vinicuncaPasada, 5, "El desayuno y el almuerzo buffet muy ricos. Puntualidad en el recojo."),
            CrearResena(ana, ballestasPasada, 5, "Vimos pingüinos y cientos de lobos marinos. La lancha era cómoda y segura."),
            CrearResena(lucia, ballestasPasada, 4, "Excelente guía. El recorrido en la reserva fue un poco corto, pero valió la pena."));

        await db.SaveChangesAsync(ct);
    }

    private static Usuario CrearEmpresa(string nombreComercial, string razonSocial, string ruc, string correo,
        string nombre, string apellidos, string telefono, string direccion, string descripcion, string hash) => new()
    {
        Nombre = nombre, Apellidos = apellidos, Correo = correo, Telefono = telefono, PasswordHash = hash, Rol = Rol.Empresa,
        Empresa = new Empresa
        {
            NombreComercial = nombreComercial, RazonSocial = razonSocial, Ruc = ruc, Telefono = telefono,
            Direccion = direccion, Descripcion = descripcion,
        },
    };

    private static Usuario CrearTurista(string nombre, string apellidos, string dni, string correo, string? telefono, string hash) => new()
    {
        Nombre = nombre, Apellidos = apellidos, Dni = dni, Correo = correo, Telefono = telefono, PasswordHash = hash, Rol = Rol.Turista,
    };

    private static Actividad CrearActividad(Empresa? empresa, string nombre, string destino, DateOnly inicio, int dias,
        decimal precio, int capacidad, string descripcion, string puntoEncuentro, string? incluye, string? noIncluye,
        params (string Titulo, string Descripcion)[] itinerario) => new()
    {
        Empresa = empresa!, Nombre = nombre, Destino = destino, FechaInicio = inicio, FechaFin = inicio.AddDays(dias - 1),
        Precio = precio, Capacidad = capacidad, Descripcion = descripcion, PuntoEncuentro = puntoEncuentro,
        Incluye = incluye, NoIncluye = noIncluye,
        Itinerario = itinerario.Select((d, i) => new ItinerarioDia { Dia = i + 1, Titulo = d.Titulo, Descripcion = d.Descripcion }).ToList(),
    };

    private Reserva Reservar(Usuario turista, Actividad actividad, int personas, EstadoReserva estado)
    {
        var reserva = new Reserva
        {
            Codigo = CodigoReserva.Generar(), Usuario = turista, Actividad = actividad, CantidadPersonas = personas,
            PrecioUnitario = actividad.Precio, Total = actividad.Precio * personas, Estado = estado,
        };
        actividad.CuposReservados += personas;
        db.Reservas.Add(reserva);
        return reserva;
    }

    private static Resena CrearResena(Usuario turista, Actividad actividad, int valoracion, string comentario) => new()
    {
        Usuario = turista, Actividad = actividad, Valoracion = valoracion, Comentario = comentario,
    };
}
