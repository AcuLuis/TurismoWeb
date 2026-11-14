using System.Globalization;
using System.Text;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Domain;
using TurismoGo.Domain.Entidades;

namespace TurismoGo.Infrastructure.Datos;

/// <summary>
/// Datos de demostración (solo desarrollo) para que la plataforma luzca "en uso": varias empresas, decenas de
/// viajeros, tours con salidas pasadas y próximas, cientos de reservas en los últimos meses y reseñas.
/// Todo es ficticio y coherente con las reglas del negocio (cupos, estados, fechas). La semilla es fija:
/// siempre se generan los mismos datos. Contraseña de todas las cuentas: <see cref="PasswordDemo"/>.
/// </summary>
internal sealed class DatosDemo(TurismoDbContext db, IHasherContrasenas hasher, IReloj reloj)
{
    public const string PasswordDemo = "Demo2026!";

    private readonly Random _rnd = new(2026);
    private readonly HashSet<string> _dnis = [];
    private readonly HashSet<string> _correos = [];
    private DateOnly _hoy;
    private DateTime _ahora;
    private string _hash = string.Empty;

    // ======================================================================
    //  Orquestación
    // ======================================================================

    public async Task SembrarAsync(CancellationToken ct)
    {
        _hoy = reloj.Hoy;
        _ahora = reloj.UtcAhora;
        _hash = hasher.Hash(PasswordDemo);

        var empresas = Empresas.Select(CrearEmpresa).ToList();
        var turistas = CrearTuristas();
        var principales = turistas.Take(3).ToHashSet();   // Ana, Carlos y Lucía (documentadas en el README)
        var salidas = CrearSalidas(empresas);
        var reservas = new List<Reserva>();
        foreach (var s in salidas.Where(s => s.Actividad.Estado == EstadoActividad.Publicada))
            reservas.AddRange(CrearReservas(s, turistas));

        GarantizarReservasDeCuentasPrincipales(principales, salidas, reservas);
        var resenas = CrearResenas(salidas, reservas);
        DejarResenaPendiente(turistas[2], reservas, resenas);   // Lucía puede probar el flujo de reseña
        AgregarResenasModeradas(reservas, resenas, principales);
        CancelarSalidaPorLaEmpresa(salidas, reservas);
        AjustarFechas(turistas, salidas, reservas);

        db.Usuarios.AddRange(empresas.Select(e => e.Usuario));
        db.Usuarios.AddRange(turistas);
        db.Actividades.AddRange(salidas.Select(s => s.Actividad));
        db.Reservas.AddRange(reservas);
        db.Resenas.AddRange(resenas);

        db.ConservarFechasHistoricas = true;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            db.ConservarFechasHistoricas = false;
        }
    }

    // ======================================================================
    //  Empresas
    // ======================================================================

    private sealed record DatosEmpresa(
        string NombreComercial, string RazonSocial, string Ruc, string Correo, string Nombre, string Apellidos,
        string Telefono, string Direccion, string Descripcion, EstadoEmpresa Estado, string? Motivo, int DiasAntiguedad);

    private static readonly DatosEmpresa[] Empresas =
    [
        new("Andes Explorer", "Andes Explorer Tours S.A.C.", "20601234567", "ventas@andesexplorer.pe", "Rosa", "Huamán Ccori",
            "984112233", "Calle Plateros 345, Cusco",
            "Operador cusqueño con 12 años de experiencia en trekking y turismo cultural en los Andes del sur.", EstadoEmpresa.Verificada, null, 480),
        new("Costa Viva", "Costa Viva Expediciones E.I.R.L.", "20509876543", "reservas@costaviva.pe", "Jorge", "Salazar Ríos",
            "956778899", "Av. Paracas 120, Pisco, Ica",
            "Aventura y naturaleza en la costa peruana: desierto, islas y gastronomía.", EstadoEmpresa.Verificada, null, 420),
        new("Amazonía Viva", "Amazonía Viva Turismo S.A.C.", "20611122233", "contacto@amazoniaviva.pe", "Milagros", "Panduro Vela",
            "965443322", "Malecón Tarapacá 210, Iquitos",
            "Expediciones a la selva baja peruana.", EstadoEmpresa.Pendiente, null, 5),
        new("Inka Trails Perú", "Inka Trails Perú S.A.C.", "20605544331", "reservas@inkatrailsperu.pe", "Wilber", "Condori Mamani",
            "974556120", "Av. El Sol 820, Cusco",
            "Especialistas en caminatas de alta montaña: Salkantay, Humantay y el Valle Sagrado.", EstadoEmpresa.Verificada, null, 360),
        new("Titicaca Nativa", "Titicaca Nativa Turismo Vivencial E.I.R.L.", "20604477120", "hola@titicacanativa.pe", "Yolanda", "Apaza Quispe",
            "951334870", "Jr. Lima 455, Puno",
            "Turismo vivencial con comunidades del lago Titicaca. Parte de lo recaudado vuelve a las familias anfitrionas.", EstadoEmpresa.Verificada, null, 330),
        new("Cañón & Volcanes", "Cañón y Volcanes Adventure S.A.C.", "20603318452", "reservas@canonyvolcanes.pe", "Ricardo", "Zegarra Linares",
            "959120774", "Calle Jerusalén 302, Arequipa",
            "Tours y ascensos en Arequipa con guías certificados de alta montaña.", EstadoEmpresa.Verificada, null, 300),
        new("Tambopata Wild", "Tambopata Wild Expeditions S.A.C.", "20607789014", "info@tambopatawild.pe", "Kelly", "Ríos Huaypuna",
            "982665410", "Av. Dos de Mayo 610, Puerto Maldonado",
            "Expediciones en la Reserva Nacional Tambopata con lodges de bajo impacto.", EstadoEmpresa.Verificada, null, 300),
        new("Norte Ancestral", "Norte Ancestral Viajes S.A.C.", "20602211876", "ventas@norteancestral.pe", "César", "Alva Rodríguez",
            "948223190", "Jr. Pizarro 512, Trujillo",
            "Arqueología y naturaleza del norte peruano: Chan Chan, Sipán y Kuélap.", EstadoEmpresa.Verificada, null, 280),
        new("Lima Sabores", "Lima Sabores Experiencias E.I.R.L.", "20608843215", "tours@limasabores.pe", "Daniela", "Ferrer Tello",
            "997441288", "Av. Larco 1150, Miraflores, Lima",
            "Experiencias gastronómicas en Lima: mercados, clases de cocina y rutas del pisco.", EstadoEmpresa.Verificada, null, 270),
        new("Ruta Express Tours", "Ruta Express Tours S.A.C.", "20601987345", "contacto@rutaexpress.pe", "Marco", "Benavides Soto",
            "913552904", "Av. Abancay 980, Lima",
            "Tours económicos de un día desde Lima.", EstadoEmpresa.Suspendida,
            "Reclamos reiterados de clientes por cancelaciones sin aviso", 360),
        new("Huaraz Andino", "Huaraz Andino Aventura S.A.C.", "20612034567", "info@huarazandino.pe", "Edwin", "Valverde Cáceres",
            "943781120", "Av. Luzuriaga 760, Huaraz",
            "Trekking y escalada en la Cordillera Blanca.", EstadoEmpresa.Pendiente, null, 2),
    ];

    private Empresa CrearEmpresa(DatosEmpresa d)
    {
        var alta = _ahora.AddDays(-d.DiasAntiguedad).AddHours(-_rnd.Next(0, 10));
        _correos.Add(d.Correo);
        var usuario = new Usuario
        {
            Nombre = d.Nombre, Apellidos = d.Apellidos, Correo = d.Correo, Telefono = d.Telefono, PasswordHash = _hash,
            Rol = Rol.Empresa, CreadoEn = alta, UltimoAccesoEn = _ahora.AddHours(-_rnd.Next(1, 96)),
        };
        var empresa = new Empresa
        {
            Usuario = usuario, RazonSocial = d.RazonSocial, NombreComercial = d.NombreComercial, Ruc = d.Ruc,
            Telefono = d.Telefono, Direccion = d.Direccion, Descripcion = d.Descripcion, Estado = d.Estado,
            MotivoEstado = d.Motivo, CreadoEn = alta,
            VerificadaEn = d.Estado == EstadoEmpresa.Pendiente ? null : alta.AddDays(_rnd.Next(1, 4)),
        };
        usuario.Empresa = empresa;
        return empresa;
    }

    // ======================================================================
    //  Viajeros
    // ======================================================================

    private static readonly string[] Nombres =
    [
        "José", "María", "Luis", "Rosa", "Jorge", "Carmen", "Miguel", "Patricia", "Juan", "Elena", "Diego", "Sofía", "Renato",
        "Valeria", "Andrés", "Camila", "Fernando", "Gabriela", "Ricardo", "Daniela", "Óscar", "Mariana", "Hugo", "Fiorella",
        "Raúl", "Milagros", "Gonzalo", "Ximena", "Alonso", "Claudia", "Martín", "Paola", "Sebastián", "Lorena", "Víctor",
        "Natalia", "Iván", "Andrea", "Pablo", "Teresa",
    ];

    private static readonly string[] Apellidos =
    [
        "Quispe", "Mamani", "Flores", "Rodríguez", "García", "Huamán", "Chávez", "Ramos", "Torres", "Vargas", "Mendoza",
        "Castillo", "Rojas", "Gutiérrez", "Salazar", "Paredes", "Cárdenas", "Silva", "Romero", "Díaz", "Ccori", "Condori",
        "Pérez", "Sánchez", "Vega", "Herrera", "Medina", "Aguilar", "Ríos", "Espinoza",
    ];

    private List<Usuario> CrearTuristas()
    {
        // Cuentas principales (documentadas en el README)
        var lista = new List<Usuario>
        {
            Turista("Ana", "Quispe Mamani", "45678912", "ana.quispe@correo.pe", "987654321"),
            Turista("Carlos", "Mendoza Paredes", "41237895", "carlos.mendoza@correo.pe", "912345678"),
            Turista("Lucía", "Torres Vargas", "47851236", "lucia.torres@correo.pe", null),
        };

        while (lista.Count < 60)
        {
            var nombre = Nombres[_rnd.Next(Nombres.Length)];
            var ap1 = Apellidos[_rnd.Next(Apellidos.Length)];
            var ap2 = Apellidos[_rnd.Next(Apellidos.Length)];
            if (ap1 == ap2) continue;
            var correo = $"{SinTildes(nombre)}.{SinTildes(ap1)}".ToLowerInvariant() + "@correo.pe";
            if (_correos.Contains(correo))
                correo = correo.Replace("@", _rnd.Next(10, 99).ToString(CultureInfo.InvariantCulture) + "@", StringComparison.Ordinal);
            var dni = _rnd.Next(40_000_000, 79_999_999).ToString(CultureInfo.InvariantCulture);
            if (_correos.Contains(correo) || _dnis.Contains(dni)) continue;
            var telefono = _rnd.NextDouble() < 0.8 ? $"9{_rnd.Next(10_000_000, 99_999_999)}" : null;
            lista.Add(Turista(nombre, $"{ap1} {ap2}", dni, correo, telefono));
        }

        // Una cuenta suspendida para el panel de administración
        lista[17].Activo = false;
        return lista;
    }

    private Usuario Turista(string nombre, string apellidos, string dni, string correo, string? telefono)
    {
        _dnis.Add(dni);
        _correos.Add(correo);
        return new Usuario
        {
            Nombre = nombre, Apellidos = apellidos, Dni = dni, Correo = correo, Telefono = telefono,
            PasswordHash = _hash, Rol = Rol.Turista,
        };
    }

    // ======================================================================
    //  Tours y salidas
    // ======================================================================

    private sealed record Tour(
        string Clave, int Empresa, string Nombre, string Destino, int Dias, decimal Precio, int Capacidad,
        double Calidad, int Pasadas, int Futuras, int CadaDias, string Descripcion, string PuntoEncuentro,
        string? Incluye, string? NoIncluye, (string Titulo, string Texto)[] Itinerario);

    private sealed record Salida(Actividad Actividad, Tour Tour);

    private static readonly Tour[] Tours =
    [
        // --- Andes Explorer ---
        new("camino-inca", 0, "Camino Inca clásico 4D/3N", "Cusco", 4, 2450m, 16, 4.8, 3, 2, 21,
            "Recorre el legendario Camino Inca hasta Machu Picchu, atravesando sitios arqueológicos, bosque nuboso y el paso de Warmiwañusca. Grupos reducidos con guía bilingüe certificado.",
            "Hotel en el centro histórico de Cusco (recojo 4:30 a. m.)",
            "Permisos de ingreso, guía, cocinero, porteadores, carpas, alimentación completa, bus de bajada y tren de retorno",
            "Bolsa de dormir, propinas, ingreso a Huayna Picchu",
            [("Cusco – Wayllabamba", "Traslado al km 82 e inicio de la caminata por el valle del Urubamba hasta Wayllabamba (12 km)."),
             ("Paso de Warmiwañusca", "Ascenso al punto más alto de la ruta (4200 m s. n. m.) y descenso a Pacaymayo."),
             ("Sitios arqueológicos", "Visita a Runkurakay, Sayacmarca y Phuyupatamarca. Noche en Wiñay Wayna."),
             ("Machu Picchu", "Llegada por la Puerta del Sol al amanecer, visita guiada y retorno en tren a Cusco.")]),
        new("machu-picchu", 0, "Machu Picchu full day en tren", "Cusco", 1, 890m, 20, 4.6, 4, 3, 12,
            "Excursión de un día a la ciudadela de Machu Picchu en tren panorámico desde Ollantaytambo, con visita guiada de dos horas.",
            "Estación de Ollantaytambo (5:45 a. m.)", "Tren ida y vuelta, bus Aguas Calientes – Machu Picchu, ingreso, guía",
            "Almuerzo, traslado Cusco – Ollantaytambo",
            [("Machu Picchu", "Tren a Aguas Calientes, subida en bus, visita guiada y tiempo libre. Retorno por la tarde.")]),
        new("vinicunca", 0, "Montaña de Colores (Vinicunca)", "Cusco", 1, 120m, 25, 4.5, 6, 4, 7,
            "Caminata a la famosa Montaña de Siete Colores, a 5036 m s. n. m., con vistas al nevado Ausangate.",
            "Plaza San Francisco, Cusco (4:00 a. m.)", "Transporte, desayuno y almuerzo buffet, guía, botiquín y oxígeno",
            "Ingreso a la montaña (S/ 25), caballo de alquiler",
            [("Vinicunca", "Viaje a Quesiuno, caminata de 2 h hasta el mirador, descenso y almuerzo en Cusipata.")]),
        new("titicaca", 0, "Lago Titicaca: Uros y Taquile 2D/1N", "Puno", 2, 340m, 18, 4.4, 3, 2, 14,
            "Navega el lago navegable más alto del mundo, visita las islas flotantes de los Uros y convive con una familia en Amantaní.",
            "Puerto de Puno (7:00 a. m.)", "Lancha, guía, alojamiento rural, tres comidas", "Traslado hotel – puerto",
            [("Uros y Amantaní", "Islas flotantes de los Uros, navegación a Amantaní y noche con familia anfitriona."),
             ("Taquile", "Caminata por la isla de Taquile, almuerzo y retorno a Puno.")]),
        new("colca", 0, "Cañón del Colca 2D/1N", "Arequipa", 2, 420m, 20, 4.5, 3, 2, 14,
            "Uno de los cañones más profundos del mundo: miradores, baños termales y el vuelo del cóndor.",
            "Plaza de Armas de Arequipa (8:00 a. m.)", "Transporte turístico, hotel en Chivay, desayuno, guía",
            "Boleto turístico del Colca, almuerzos",
            [("Arequipa – Chivay", "Reserva de Salinas y Aguada Blanca, Patapampa y baños termales de La Calera."),
             ("Cruz del Cóndor", "Avistamiento de cóndores, pueblos de Maca y Yanque, retorno a Arequipa.")]),

        // --- Costa Viva ---
        new("ballestas", 1, "Islas Ballestas y Reserva de Paracas", "Ica", 1, 180m, 30, 4.6, 5, 3, 9,
            "Paseo en lancha a las Islas Ballestas para ver lobos marinos y pingüinos de Humboldt, y recorrido por la Reserva Nacional de Paracas.",
            "Muelle El Chaco, Paracas (8:00 a. m.)", "Lancha, chalecos, guía, bus en la reserva", "Tasa de embarque e ingreso a la reserva",
            [("Paracas", "Islas Ballestas por la mañana; tarde en la Reserva de Paracas con la playa Roja y el mirador de La Catedral.")]),
        new("huacachina", 1, "Huacachina: tubulares y sandboard", "Ica", 1, 95m, 24, 4.7, 5, 3, 8,
            "Recorrido en buggies por las dunas que rodean el oasis de Huacachina, con práctica de sandboard y atardecer en el desierto.",
            "Laguna de Huacachina (3:30 p. m.)", "Tubular, tabla de sandboard, conductor experimentado", "Tasa municipal (S/ 3.50)",
            [("Desierto de Ica", "Dos horas de dunas, sandboard en tres pendientes y atardecer.")]),
        new("lima-colonial", 1, "Lima colonial y gastronómica", "Lima", 1, 150m, 15, 4.5, 4, 3, 10,
            "Centro histórico de Lima, catacumbas de San Francisco y una ruta de degustación por el Barrio Chino y el Mercado Central.",
            "Plaza Mayor de Lima (9:30 a. m.)", "Guía, degustaciones, ingreso a San Francisco", "Traslados desde el hotel",
            [("Centro de Lima", "Plaza Mayor, Catedral, San Francisco y ruta gastronómica.")]),
        new("laguna69", 1, "Trekking Laguna 69", "Áncash", 1, 160m, 14, 4.8, 3, 3, 12,
            "Una de las caminatas más bellas de la Cordillera Blanca, hasta la laguna turquesa al pie del nevado Chacraraju.",
            "Plaza de Armas de Huaraz (5:00 a. m.)", "Transporte, guía de montaña, desayuno", "Ingreso al Parque Nacional Huascarán, almuerzo",
            [("Laguna 69", "Viaje a Cebollapampa, ascenso de 3 h a la laguna, descenso y retorno a Huaraz.")]),

        // --- Inka Trails Perú ---
        new("valle-sagrado", 3, "Valle Sagrado de los Incas full day", "Cusco", 1, 180m, 20, 4.6, 5, 3, 7,
            "Pisac, Urubamba, Ollantaytambo y Chinchero en un solo día, con almuerzo buffet y guía bilingüe.",
            "Plaza de Armas de Cusco (7:00 a. m.)", "Transporte turístico, guía bilingüe, almuerzo buffet", "Boleto turístico del Cusco",
            [("Valle Sagrado", "Mercado de Pisac, almuerzo en Urubamba, fortaleza de Ollantaytambo y textiles de Chinchero.")]),
        new("humantay", 3, "Laguna Humantay", "Cusco", 1, 140m, 20, 4.7, 5, 3, 7,
            "Caminata a la laguna turquesa de Humantay, al pie del nevado del mismo nombre, en la ruta al Salkantay.",
            "Hotel en Cusco (4:30 a. m.)", "Transporte, desayuno y almuerzo, guía, bastones", "Ingreso a la laguna (S/ 20), caballo",
            [("Humantay", "Viaje a Soraypampa, ascenso de 1 h 30 min a la laguna, almuerzo en Mollepata.")]),
        new("salkantay", 3, "Salkantay Trek 5D/4N a Machu Picchu", "Cusco", 5, 2100m, 12, 4.8, 2, 2, 21,
            "La alternativa más espectacular a la ruta clásica: glaciares, selva alta y la llegada a Machu Picchu.",
            "Hotel en Cusco (4:00 a. m.)", "Guía, arrieros, mulas, carpas, alimentación, ingreso a Machu Picchu, tren de retorno",
            "Bolsa de dormir, propinas, último almuerzo",
            [("Mollepata – Soraypampa", "Traslado e inicio de la caminata; visita a la laguna Humantay."),
             ("Paso Salkantay", "Ascenso al paso (4600 m s. n. m.) y descenso a Chaullay."),
             ("Selva alta", "Caminata entre cafetales y cascadas hasta La Playa."),
             ("Hidroeléctrica – Aguas Calientes", "Caminata junto a las vías del tren hasta Aguas Calientes."),
             ("Machu Picchu", "Visita guiada a la ciudadela y retorno en tren.")]),
        new("city-cusco", 3, "City tour Cusco y Sacsayhuamán", "Cusco", 1, 70m, 25, 4.3, 5, 3, 6,
            "Qoricancha, Catedral y los cuatro sitios arqueológicos cercanos a Cusco: Sacsayhuamán, Qenqo, Puca Pucara y Tambomachay.",
            "Plaza Regocijo, Cusco (1:30 p. m.)", "Transporte, guía", "Boleto turístico, ingreso a Qoricancha y Catedral",
            [("Cusco imperial", "Visita a Qoricancha, Catedral y sitios arqueológicos de los alrededores.")]),

        // --- Titicaca Nativa ---
        new("amantani", 4, "Uros, Amantaní y Taquile vivencial 2D/1N", "Puno", 2, 320m, 16, 4.8, 4, 2, 10,
            "Turismo vivencial con familias quechuas de Amantaní: cocina local, tejido y una noche bajo el cielo del altiplano.",
            "Puerto de Puno (7:30 a. m.)", "Lancha, guía local, alojamiento con familia, todas las comidas", "Propinas",
            [("Uros y Amantaní", "Visita a los Uros, llegada a Amantaní, cena con la familia y fiesta tradicional."),
             ("Taquile", "Caminata por Taquile, almuerzo con trucha fresca y retorno.")]),
        new("kayak-titicaca", 4, "Kayak en el lago Titicaca", "Puno", 1, 210m, 10, 4.7, 4, 2, 9,
            "Rema en kayak entre los totorales de la bahía de Puno y visita una isla flotante poco concurrida.",
            "Muelle de Chimu, Puno (8:00 a. m.)", "Kayak doble, chaleco, guía, snack", "Traslado al muelle",
            [("Bahía de Puno", "Remada de 3 h, visita a una isla flotante y almuerzo ligero.")]),

        // --- Cañón & Volcanes ---
        new("arequipa-city", 5, "City tour Arequipa y Monasterio de Santa Catalina", "Arequipa", 1, 85m, 18, 4.6, 5, 3, 7,
            "La Ciudad Blanca: Plaza de Armas, miradores de Yanahuara y Carmen Alto, y el Monasterio de Santa Catalina.",
            "Plaza de Armas de Arequipa (9:00 a. m.)", "Transporte, guía, ingreso al monasterio", "Almuerzo",
            [("Arequipa", "Centro histórico, monasterio, miradores y la mansión del Fundador.")]),
        new("misti", 5, "Ascenso al volcán Misti 2D/1N", "Arequipa", 2, 650m, 8, 4.7, 3, 2, 14,
            "Ascenso técnico no especializado al volcán emblemático de Arequipa (5822 m s. n. m.) con guías de alta montaña.",
            "Oficina en Calle Jerusalén 302, Arequipa (8:00 a. m.)", "Guía de alta montaña, carpas, alimentación, transporte 4x4",
            "Equipo personal de abrigo",
            [("Campamento base", "Traslado en 4x4 y caminata hasta el campamento Monte Blanco."),
             ("Cumbre", "Ataque a la cumbre de madrugada, vista del cráter y descenso.")]),
        new("colca-termal", 5, "Colca y aguas termales 2D/1N", "Arequipa", 2, 390m, 18, 4.4, 3, 2, 12,
            "Versión relajada con más tiempo en los baños termales de Chivay y el mirador de la Cruz del Cóndor.",
            "Hotel en Arequipa (7:30 a. m.)", "Transporte, hotel con aguas termales, desayuno, guía", "Boleto turístico, almuerzos",
            [("Chivay", "Ruta por la reserva y tarde en aguas termales."),
             ("Cruz del Cóndor", "Mirador de cóndores y retorno a Arequipa.")]),

        // --- Tambopata Wild ---
        new("tambopata", 6, "Reserva Nacional Tambopata 3D/2N", "Madre de Dios", 3, 1290m, 12, 4.8, 3, 2, 14,
            "Selva amazónica en estado puro: lago Sandoval, caminatas nocturnas y avistamiento de nutrias gigantes.",
            "Aeropuerto de Puerto Maldonado", "Traslados, lodge, alimentación, guía naturalista, botes", "Vuelos, bebidas",
            [("Llegada al lodge", "Navegación por el río Madre de Dios y caminata nocturna."),
             ("Lago Sandoval", "Canotaje en el lago, nutrias gigantes y aves."),
             ("Retorno", "Collpa de loros al amanecer y traslado al aeropuerto.")]),
        new("collpa", 6, "Collpa de guacamayos 2D/1N", "Madre de Dios", 2, 580m, 10, 4.6, 3, 1, 14,
            "Uno de los espectáculos naturales más coloridos: cientos de guacamayos en la collpa de arcilla.",
            "Puerto de Infierno (6:00 a. m.)", "Bote, lodge, alimentación, guía", "Bebidas",
            [("Navegación", "Navegación río arriba y caminata por la selva."),
             ("Collpa", "Observación de guacamayos al amanecer y retorno.")]),

        // --- Norte Ancestral ---
        new("chan-chan", 7, "Chan Chan y Huanchaco", "La Libertad", 1, 120m, 20, 4.5, 4, 3, 8,
            "La ciudad de barro más grande de América, las huacas del Sol y la Luna y los caballitos de totora de Huanchaco.",
            "Plaza de Armas de Trujillo (9:00 a. m.)", "Transporte, guía, ingresos", "Almuerzo",
            [("Trujillo", "Huacas del Sol y la Luna, Chan Chan y tarde en Huanchaco.")]),
        new("kuelap", 7, "Kuélap y catarata de Gocta 3D/2N", "Amazonas", 3, 980m, 14, 4.7, 2, 2, 18,
            "La fortaleza chachapoyas de Kuélap en teleférico y la caminata a Gocta, una de las cataratas más altas del mundo.",
            "Plaza de Armas de Chachapoyas (8:00 a. m.)", "Transporte, hotel, teleférico, guía, desayunos", "Almuerzos y cenas",
            [("Kuélap", "Teleférico y visita guiada a la fortaleza."),
             ("Gocta", "Caminata de 5 h a la catarata."),
             ("Karajía", "Sarcófagos de Karajía y retorno.")]),
        new("sipan", 7, "Señor de Sipán y Túcume", "Lambayeque", 1, 150m, 20, 4.6, 3, 2, 10,
            "Museo Tumbas Reales de Sipán, Huaca Rajada y el valle de las pirámides de Túcume.",
            "Plaza de Armas de Chiclayo (8:30 a. m.)", "Transporte, guía, ingresos", "Almuerzo",
            [("Lambayeque", "Museo Tumbas Reales, Huaca Rajada y Túcume.")]),

        // --- Lima Sabores ---
        new("clase-cocina", 8, "Clase de cocina peruana y mercado", "Lima", 1, 220m, 12, 4.9, 6, 4, 6,
            "Compra ingredientes en el mercado de Surquillo y prepara ceviche, lomo saltado y pisco sour con un chef local.",
            "Mercado N.° 1 de Surquillo (10:00 a. m.)", "Ingredientes, clase, almuerzo, recetario", "Traslados",
            [("Mercado y cocina", "Recorrido por el mercado, clase práctica y almuerzo con lo preparado.")]),
        new("pisco", 8, "Ruta del pisco en Lunahuaná", "Lima", 1, 190m, 16, 4.4, 3, 2, 14,
            "Bodegas artesanales de pisco, canotaje opcional en el río Cañete y almuerzo campestre.",
            "Óvalo Gutiérrez, Miraflores (7:00 a. m.)", "Transporte, degustaciones, almuerzo, guía", "Canotaje",
            [("Lunahuaná", "Visita a dos bodegas, almuerzo y tarde libre en el pueblo.")]),

        // --- Ruta Express Tours (suspendida): solo historial ---
        new("express", 9, "Paracas e Ica express", "Ica", 1, 99m, 40, 2.7, 4, 0, 10,
            "Tour económico de un día a Paracas, Huacachina y una bodega de pisco.",
            "Av. Abancay 980, Lima (4:00 a. m.)", "Transporte, guía", "Lancha, almuerzo, ingresos",
            [("Ica", "Paracas, Huacachina y bodega de pisco.")]),
    ];

    private List<Salida> CrearSalidas(List<Empresa> empresas)
    {
        var salidas = new List<Salida>();
        foreach (var tour in Tours)
        {
            var empresa = empresas[tour.Empresa];

            // Próximas salidas
            var inicio = _hoy.AddDays(_rnd.Next(2, 7));
            for (var i = 0; i < tour.Futuras; i++)
            {
                salidas.Add(new Salida(NuevaActividad(tour, empresa, inicio, EstadoActividad.Publicada), tour));
                inicio = inicio.AddDays(tour.CadaDias + _rnd.Next(0, 3));
            }

            // Salidas ya realizadas (últimos ~5 meses)
            var inicioPasado = _hoy.AddDays(-tour.Dias - _rnd.Next(2, 6));
            for (var i = 0; i < tour.Pasadas && inicioPasado > _hoy.AddDays(-150); i++)
            {
                salidas.Add(new Salida(NuevaActividad(tour, empresa, inicioPasado, EstadoActividad.Publicada), tour));
                inicioPasado = inicioPasado.AddDays(-tour.CadaDias - _rnd.Next(0, 4));
            }
        }

        // Borradores (en preparación), incluidas las actividades de empresas pendientes de verificación
        Tour Borrador(int emp, string nombre, string destino, int dias, decimal precio, int cap, string desc, string punto) =>
            new($"borrador-{nombre}", emp, nombre, destino, dias, precio, cap, 4.5, 0, 0, 0, desc, punto, null, null,
                [("Día 1", "Itinerario en preparación.")]);
        foreach (var b in new[]
                 {
                     Borrador(0, "Ausangate trek 6D/5N", "Cusco", 6, 2890m, 10, "Trekking de alta montaña alrededor del nevado Ausangate, con lagunas de colores y aguas termales.", "Hotel en Cusco"),
                     Borrador(1, "Kayak en la bahía de Paracas", "Ica", 1, 130m, 10, "Recorrido en kayak por la bahía de Paracas con avistamiento de aves marinas.", "Playa El Chaco"),
                     Borrador(8, "Tour nocturno de picanterías", "Lima", 1, 160m, 12, "Recorrido nocturno por picanterías y anticucheras tradicionales de Barranco.", "Puente de los Suspiros"),
                     Borrador(2, "Expedición Pacaya Samiria 4D/3N", "Loreto", 4, 1650m, 12, "Navegación por la reserva Pacaya Samiria con avistamiento de delfines rosados.", "Puerto de Nauta"),
                     Borrador(10, "Nevado Pastoruri y Puya Raimondi", "Áncash", 1, 110m, 20, "Glaciar Pastoruri y el bosque de Puya Raimondi en el Parque Nacional Huascarán.", "Plaza de Armas de Huaraz"),
                 })
        {
            salidas.Add(new Salida(NuevaActividad(b, empresas[b.Empresa], _hoy.AddDays(_rnd.Next(25, 50)), EstadoActividad.Borrador), b));
        }

        return salidas;
    }

    private static Actividad NuevaActividad(Tour t, Empresa empresa, DateOnly inicio, EstadoActividad estado) => new()
    {
        Empresa = empresa, Nombre = t.Nombre, Destino = t.Destino, Descripcion = t.Descripcion, PuntoEncuentro = t.PuntoEncuentro,
        FechaInicio = inicio, FechaFin = inicio.AddDays(t.Dias - 1), Precio = t.Precio, Capacidad = t.Capacidad,
        Incluye = t.Incluye, NoIncluye = t.NoIncluye, Estado = estado,
        Itinerario = t.Itinerario.Select((d, i) => new ItinerarioDia { Dia = i + 1, Titulo = d.Titulo, Descripcion = d.Texto }).ToList(),
    };

    // ======================================================================
    //  Reservas
    // ======================================================================

    private static readonly string[] Notas =
    [
        "Soy vegetariana, por favor considerar en el almuerzo.", "Viajamos con un niño de 8 años.",
        "¿Pueden recogernos en el hotel? Estamos en el centro histórico.", "Alergia a los mariscos.",
        "Celebramos nuestro aniversario.", "Necesito factura a nombre de empresa.", "Llegamos un día antes para aclimatarnos.",
    ];

    private static readonly string[] MotivosCancelacion =
    [
        "Cambio de planes del cliente.", "Problemas de salud.", "Se reprogramó el vuelo.", "Viaje pospuesto por motivos laborales.",
    ];

    private static readonly string[] MotivosRechazo =
    [
        "No hay disponibilidad de guía para un grupo de ese tamaño.", "La reserva supera la capacidad de la movilidad asignada.",
        "No pudimos contactar al cliente para confirmar sus datos.",
    ];

    private List<Reserva> CrearReservas(Salida s, List<Usuario> turistas)
    {
        var a = s.Actividad;
        var pasada = a.FechaFin < _hoy;
        var dias = a.FechaInicio.DayNumber - _hoy.DayNumber;
        var ocupacion = pasada ? Rango(0.45, 0.95)
            : dias <= 7 ? Rango(0.45, 0.85)
            : dias <= 21 ? Rango(0.2, 0.6)
            : Rango(0.05, 0.35);
        var objetivo = (int)Math.Round(a.Capacidad * ocupacion);
        var usados = new HashSet<Usuario>();
        var lista = new List<Reserva>();

        for (var intento = 0; a.CuposReservados < objetivo && intento < 300; intento++)
        {
            var turista = turistas[_rnd.Next(turistas.Count)];
            if ((!turista.Activo && !pasada) || !usados.Add(turista)) continue;
            var personas = Math.Min(Personas(), a.Capacidad - a.CuposReservados);
            if (personas <= 0) break;
            lista.Add(NuevaReserva(a, turista, personas, EstadoAleatorio(pasada, dias)));
        }
        return lista;
    }

    private Reserva NuevaReserva(Actividad a, Usuario turista, int personas, EstadoReserva estado)
    {
        var pasada = a.FechaFin < _hoy;
        var creada = pasada
            ? MomentoDelDia(a.FechaInicio.AddDays(-_rnd.Next(2, 61)))
            : MomentoDelDia(_hoy.AddDays(-_rnd.Next(0, Math.Min(45, 10 + a.FechaInicio.DayNumber - _hoy.DayNumber))));
        if (creada > _ahora) creada = _ahora.AddMinutes(-_rnd.Next(30, 600));

        var r = new Reserva
        {
            Codigo = CodigoReserva.Generar(), Usuario = turista, Actividad = a, CantidadPersonas = personas,
            PrecioUnitario = a.Precio, Total = a.Precio * personas, Estado = estado, CreadoEn = creada,
            Notas = _rnd.NextDouble() < 0.12 ? Notas[_rnd.Next(Notas.Length)] : null,
        };

        if (estado == EstadoReserva.Cancelada)
        {
            r.MotivoEstado = MotivosCancelacion[_rnd.Next(MotivosCancelacion.Length)];
            r.CanceladaEn = Min(creada.AddDays(_rnd.Next(1, 8)), _ahora, a.FechaInicio.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            if (r.CanceladaEn < creada) r.CanceladaEn = creada.AddHours(2);
        }
        else if (estado == EstadoReserva.Rechazada)
        {
            r.MotivoEstado = MotivosRechazo[_rnd.Next(MotivosRechazo.Length)];
        }

        if (r.EstaActiva) a.CuposReservados += personas;
        return r;
    }

    private EstadoReserva EstadoAleatorio(bool pasada, int diasHastaInicio)
    {
        var x = _rnd.NextDouble();
        if (pasada) return x < 0.88 ? EstadoReserva.Confirmada : EstadoReserva.Cancelada;
        if (diasHastaInicio <= 7)
            return x < 0.75 ? EstadoReserva.Confirmada : x < 0.90 ? EstadoReserva.Pendiente : x < 0.97 ? EstadoReserva.Cancelada : EstadoReserva.Rechazada;
        return x < 0.55 ? EstadoReserva.Confirmada : x < 0.90 ? EstadoReserva.Pendiente : x < 0.97 ? EstadoReserva.Cancelada : EstadoReserva.Rechazada;
    }

    private int Personas()
    {
        var x = _rnd.NextDouble();
        return x < 0.35 ? 1 : x < 0.75 ? 2 : x < 0.87 ? 3 : 4;
    }

    /// <summary>Las cuentas documentadas tienen siempre reservas próximas y actividades ya realizadas.</summary>
    private void GarantizarReservasDeCuentasPrincipales(HashSet<Usuario> principales, List<Salida> salidas, List<Reserva> reservas)
    {
        var publicadas = salidas
            .Where(s => s.Actividad.Estado == EstadoActividad.Publicada && s.Actividad.Empresa.Estado == EstadoEmpresa.Verificada)
            .ToList();
        foreach (var t in principales)
        {
            var futuras = reservas.Count(r => r.Usuario == t && r.EstaActiva && r.Actividad.FechaFin >= _hoy);
            var realizadas = reservas.Count(r => r.Usuario == t && r.Estado == EstadoReserva.Confirmada && r.Actividad.FechaFin < _hoy);
            for (; futuras < 2; futuras++)
                Agregar(t, publicadas.Where(s => s.Actividad.FechaInicio > _hoy.AddDays(4)),
                    _rnd.NextDouble() < 0.6 ? EstadoReserva.Confirmada : EstadoReserva.Pendiente);
            for (; realizadas < 3; realizadas++)
                Agregar(t, publicadas.Where(s => s.Actividad.FechaFin < _hoy), EstadoReserva.Confirmada);
        }

        void Agregar(Usuario t, IEnumerable<Salida> candidatas, EstadoReserva estado)
        {
            var s = candidatas
                .Where(c => c.Actividad.CuposDisponibles >= 2 && reservas.All(r => r.Usuario != t || r.Actividad != c.Actividad))
                .OrderBy(_ => _rnd.Next()).FirstOrDefault();
            if (s is not null) reservas.Add(NuevaReserva(s.Actividad, t, _rnd.Next(1, 3), estado));
        }
    }

    // ======================================================================
    //  Reseñas
    // ======================================================================

    private static readonly Dictionary<int, string[]> Comentarios = new()
    {
        [5] =
        [
            "Una experiencia inolvidable. El guía conocía cada detalle de la historia y nos tuvo paciencia todo el tiempo.",
            "Todo salió según lo prometido: puntualidad, buena comida y paisajes impresionantes. Lo recomiendo totalmente.",
            "Superó nuestras expectativas. El grupo era pequeño y eso hizo la experiencia mucho más personal.",
            "Excelente organización desde el recojo hasta el retorno. Volvería a reservar con esta empresa sin dudarlo.",
            "Fuimos en familia con dos niños y estuvieron pendientes de todos. Muy seguro y bien coordinado.",
            "Paisajes de otro mundo. El guía tomó fotos al grupo en los mejores puntos, un detalle que se agradece.",
            "La mejor actividad de nuestro viaje por Perú. Vale cada sol.",
            "Muy profesionales. Nos explicaron todo antes de salir y llevaban botiquín y oxígeno por si acaso.",
            "Reservé con pocos días de anticipación y me confirmaron rápido. Todo impecable.",
            "Guía bilingüe excelente, mis amigos extranjeros quedaron encantados.",
            "Comida deliciosa y abundante. El ritmo fue adecuado para todo el grupo.",
            "Increíble de principio a fin. Lo volvería a hacer mañana mismo.",
        ],
        [4] =
        [
            "Muy buena experiencia. Solo el recojo se retrasó unos 20 minutos, pero el resto fue excelente.",
            "Bonito recorrido y buen guía. El almuerzo podría mejorar, pero en general muy recomendable.",
            "Lo disfrutamos mucho. Recomiendo llevar abrigo y bloqueador, el clima cambia rápido.",
            "Buena relación calidad-precio. El bus era cómodo aunque el trayecto es largo.",
            "El paisaje compensa el madrugón. El grupo era algo grande, pero bien organizado.",
            "Todo bien en general. Nos hubiera gustado tener un poco más de tiempo libre en el destino.",
            "Guía muy amable y conocedor. La información previa por mensaje fue clara.",
            "Experiencia recomendable. Algunos tramos son exigentes, conviene aclimatarse antes.",
        ],
        [3] =
        [
            "Estuvo bien, pero el grupo era demasiado grande y era difícil escuchar al guía.",
            "El lugar es precioso, aunque la organización fue algo desordenada al inicio.",
            "Correcto, sin más. Esperaba que el almuerzo estuviera incluido como decía la descripción.",
            "Hubo demoras en varios puntos y se acortó la visita principal.",
            "Experiencia regular. El transporte no era tan cómodo como se mostraba.",
        ],
        [2] =
        [
            "Salimos con más de una hora de retraso y no nos dieron explicación. El guía se notaba apurado.",
            "El itinerario no se cumplió completo. Esperaba más por el precio pagado.",
            "Poca comunicación antes del viaje y el punto de encuentro no estaba bien indicado.",
        ],
        [1] =
        [
            "Muy mala experiencia: cambiaron el itinerario sin avisar y no respondieron nuestros reclamos.",
            "No lo recomiendo. El vehículo estaba en malas condiciones y la actividad se suspendió a mitad de camino.",
        ],
    };

    private List<Resena> CrearResenas(List<Salida> salidas, List<Reserva> reservas)
    {
        var calidad = salidas.ToDictionary(s => s.Actividad, s => s.Tour.Calidad);
        var resenas = new List<Resena>();
        foreach (var r in reservas.Where(r => r.Estado == EstadoReserva.Confirmada && r.Actividad.FechaFin < _hoy))
        {
            if (_rnd.NextDouble() > 0.65) continue;
            var ruido = (_rnd.NextDouble() + _rnd.NextDouble() - 1) * 1.6;
            var valoracion = Math.Clamp((int)Math.Round(calidad[r.Actividad] + ruido), 1, 5);
            var textos = Comentarios[valoracion];
            resenas.Add(new Resena
            {
                Usuario = r.Usuario, Actividad = r.Actividad, Valoracion = valoracion, Comentario = textos[_rnd.Next(textos.Length)],
                CreadoEn = Min(MomentoDelDia(r.Actividad.FechaFin.AddDays(_rnd.Next(1, 10))), _ahora.AddHours(-1)),
            });
        }
        return resenas;
    }

    /// <summary>Deja a la cuenta indicada con una actividad completada sin reseñar.</summary>
    private void DejarResenaPendiente(Usuario turista, List<Reserva> reservas, List<Resena> resenas)
    {
        var completadas = reservas
            .Where(r => r.Usuario == turista && r.Estado == EstadoReserva.Confirmada && r.Actividad.FechaFin < _hoy)
            .Select(r => r.Actividad).ToHashSet();
        var propias = resenas.Where(x => x.Usuario == turista && completadas.Contains(x.Actividad)).ToList();
        if (propias.Count > 0 && propias.Count == completadas.Count)
            resenas.Remove(propias.OrderByDescending(x => x.Actividad.FechaFin).First());
    }

    /// <summary>Dos reseñas ocultas por moderación, para el panel de administración.</summary>
    private void AgregarResenasModeradas(List<Reserva> reservas, List<Resena> resenas, HashSet<Usuario> excluidos)
    {
        var resenadas = resenas.Select(x => (x.Usuario, x.Actividad)).ToHashSet();
        var candidatas = reservas
            .Where(r => r.Estado == EstadoReserva.Confirmada && r.Actividad.FechaFin < _hoy && !excluidos.Contains(r.Usuario) &&
                        !resenadas.Contains((r.Usuario, r.Actividad)))
            .Skip(5).Take(2).ToList();
        var casos = new[]
        {
            ("Visiten mi página para tours más baratos, escríbanme al número que aparece en mi perfil.", 5, "Contenido promocional ajeno a la actividad."),
            ("Todos son unos estafadores y no sirven para nada, no vayan.", 1, "Lenguaje ofensivo; no describe la experiencia."),
        };
        for (var i = 0; i < candidatas.Count; i++)
        {
            var (texto, valoracion, motivo) = casos[i];
            resenas.Add(new Resena
            {
                Usuario = candidatas[i].Usuario, Actividad = candidatas[i].Actividad, Valoracion = valoracion, Comentario = texto,
                Oculta = true, MotivoOcultamiento = motivo,
                CreadoEn = Min(MomentoDelDia(candidatas[i].Actividad.FechaFin.AddDays(2)), _ahora.AddHours(-1)),
            });
        }
    }

    /// <summary>Una salida próxima cancelada por la empresa (con sus reservas canceladas y el motivo).</summary>
    private void CancelarSalidaPorLaEmpresa(List<Salida> salidas, List<Reserva> reservas)
    {
        var s = salidas.Where(x => x.Tour.Clave == "laguna69" && x.Actividad.FechaInicio > _hoy)
            .OrderByDescending(x => x.Actividad.FechaInicio).FirstOrDefault();
        if (s is null) return;
        foreach (var r in reservas.Where(r => r.Actividad == s.Actividad && r.EstaActiva))
            r.Cancelar(_ahora.AddDays(-1), "Actividad cancelada por la empresa: cierre temporal de la ruta por lluvias intensas");
        s.Actividad.Estado = EstadoActividad.Cancelada;
    }

    // ======================================================================
    //  Fechas de alta coherentes con la actividad de cada cuenta
    // ======================================================================

    private void AjustarFechas(List<Usuario> turistas, List<Salida> salidas, List<Reserva> reservas)
    {
        var porActividad = reservas.GroupBy(r => r.Actividad).ToDictionary(g => g.Key, g => g.Min(r => r.CreadoEn));
        foreach (var s in salidas)
        {
            var a = s.Actividad;
            var limite = a.FechaInicio.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(-20);
            if (porActividad.TryGetValue(a, out var primera) && primera < limite) limite = primera;
            var creada = limite.AddDays(-_rnd.Next(3, 15));
            if (creada > _ahora) creada = _ahora.AddDays(-_rnd.Next(1, 5));
            a.CreadoEn = Max(creada, a.Empresa.CreadoEn.AddDays(1));
        }

        var porTurista = reservas.GroupBy(r => r.Usuario)
            .ToDictionary(g => g.Key, g => (Primera: g.Min(r => r.CreadoEn), Ultima: g.Max(r => r.CreadoEn)));
        foreach (var t in turistas)
        {
            if (porTurista.TryGetValue(t, out var f))
            {
                t.CreadoEn = f.Primera.AddDays(-_rnd.Next(1, 40));
                t.UltimoAccesoEn = Min(f.Ultima.AddDays(_rnd.Next(0, 20)), _ahora.AddHours(-_rnd.Next(1, 48)));
            }
            else
            {
                t.CreadoEn = _ahora.AddDays(-_rnd.Next(1, 120));
                t.UltimoAccesoEn = Min(t.CreadoEn.AddDays(_rnd.Next(0, 3)), _ahora.AddHours(-2));
            }
        }
    }

    // ======================================================================
    //  Utilidades
    // ======================================================================

    private double Rango(double min, double max) => min + _rnd.NextDouble() * (max - min);

    /// <summary>Hora de oficina en Perú (13:00–22:59 UTC equivale a 8:00–17:59 en Lima).</summary>
    private DateTime MomentoDelDia(DateOnly fecha) =>
        fecha.ToDateTime(new TimeOnly(_rnd.Next(13, 23), _rnd.Next(0, 60)), DateTimeKind.Utc);

    private static DateTime Min(params DateTime[] valores) => valores.Min();
    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

    private static string SinTildes(string texto)
    {
        var sb = new StringBuilder();
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        }
        return sb.ToString();
    }
}
