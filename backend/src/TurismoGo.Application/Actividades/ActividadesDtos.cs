using TurismoGo.Domain;

namespace TurismoGo.Application.Actividades;

// ----- Catálogo público -----

public sealed record FiltroCatalogo(
    string? Q = null, string? Destino = null, DateOnly? Desde = null, DateOnly? Hasta = null,
    decimal? PrecioMin = null, decimal? PrecioMax = null, string? Orden = null, int? Pagina = null, int? Tamano = null);

public sealed record EmpresaResumenDto(int Id, string NombreComercial, double? Valoracion, int TotalResenas);

public sealed record ActividadResumenDto(
    int Id, string Nombre, string Destino, DateOnly FechaInicio, DateOnly FechaFin, int DuracionDias,
    decimal Precio, int Capacidad, int CuposDisponibles, string? ImagenUrl,
    double? Valoracion, int TotalResenas, EmpresaResumenDto Empresa);

public sealed record ItinerarioDiaDto(int Dia, string Titulo, string Descripcion);

public sealed record EmpresaPublicaDto(
    int Id, string NombreComercial, string? Descripcion, string? LogoUrl, double? Valoracion, int TotalResenas);

public sealed record ActividadDetalleDto(
    int Id, string Nombre, string Descripcion, string Destino, string PuntoEncuentro,
    DateOnly FechaInicio, DateOnly FechaFin, int DuracionDias, decimal Precio, int Capacidad, int CuposDisponibles,
    string? Incluye, string? NoIncluye, string? ImagenUrl, EstadoActividad Estado,
    bool Reservable, bool Finalizada, IReadOnlyList<ItinerarioDiaDto> Itinerario,
    double? Valoracion, int TotalResenas, EmpresaPublicaDto Empresa);

public sealed record ResenaDto(int Id, string Autor, int Valoracion, string Comentario, DateTime Fecha);

public sealed record ResenasActividadDto(
    double? Promedio, int Total, IReadOnlyDictionary<int, int> Distribucion,
    IReadOnlyList<ResenaDto> Items, int Pagina, int TotalPaginas);

// ----- Gestión de actividades (empresa) -----

public sealed record GuardarActividadRequest(
    string Nombre, string Descripcion, string Destino, string PuntoEncuentro,
    DateOnly FechaInicio, DateOnly FechaFin, decimal Precio, int Capacidad,
    string? Incluye, string? NoIncluye, string? ImagenUrl, List<ItinerarioDiaDto>? Itinerario);

public sealed record ActividadEmpresaFilaDto(
    int Id, string Nombre, string Destino, DateOnly FechaInicio, DateOnly FechaFin, decimal Precio,
    int Capacidad, int CuposReservados, EstadoActividad Estado, bool Finalizada, int ReservasPendientes, string? ImagenUrl);

public sealed record ActividadFormularioDto(
    int Id, string Nombre, string Descripcion, string Destino, string PuntoEncuentro,
    DateOnly FechaInicio, DateOnly FechaFin, decimal Precio, int Capacidad, int CuposReservados,
    string? Incluye, string? NoIncluye, string? ImagenUrl, EstadoActividad Estado,
    bool TieneReservasActivas, bool Finalizada, IReadOnlyList<ItinerarioDiaDto> Itinerario);

public sealed record MotivoRequest(string? Motivo);

public sealed record ProximaSalidaDto(int ActividadId, string Nombre, DateOnly FechaInicio, int Capacidad, int CuposReservados, int ReservasPendientes);

public sealed record PanelEmpresaDto(
    EstadoEmpresa Estado, int ActividadesPublicadas, int ActividadesBorrador, int ReservasPendientes,
    int ReservasMes, int PersonasMes, decimal IngresosMes, double? Valoracion, int TotalResenas,
    IReadOnlyList<ProximaSalidaDto> ProximasSalidas);

public sealed record ArchivoDto(byte[] Contenido, string TipoContenido, string Nombre);
