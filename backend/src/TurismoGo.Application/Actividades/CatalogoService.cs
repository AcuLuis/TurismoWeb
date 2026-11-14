using Microsoft.EntityFrameworkCore;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Application.Comun;
using TurismoGo.Domain;
using TurismoGo.Domain.Entidades;

namespace TurismoGo.Application.Actividades;

/// <summary>Catálogo público de actividades (RQ-004 y RQ-005).</summary>
public interface ICatalogoService
{
    Task<Paginado<ActividadResumenDto>> BuscarAsync(FiltroCatalogo filtro, CancellationToken ct = default);
    Task<IReadOnlyList<string>> DestinosAsync(CancellationToken ct = default);
    Task<ActividadDetalleDto> DetalleAsync(int id, CancellationToken ct = default);
    Task<ResenasActividadDto> ResenasAsync(int actividadId, int? pagina, CancellationToken ct = default);
}

internal sealed class CatalogoService(IAppDbContext db, IReloj reloj, IUsuarioActual usuarioActual) : ICatalogoService
{
    private static readonly string[] Ordenes = ["fecha", "precio-asc", "precio-desc", "valoracion"];

    /// <summary>Actividades visibles públicamente: publicadas, de empresas verificadas.</summary>
    private IQueryable<Actividad> Visibles() => db.Actividades.AsNoTracking()
        .Where(a => a.Estado == EstadoActividad.Publicada && a.Empresa.Estado == EstadoEmpresa.Verificada);

    public async Task<Paginado<ActividadResumenDto>> BuscarAsync(FiltroCatalogo filtro, CancellationToken ct = default)
    {
        ValidarFiltro(filtro);
        var hoy = reloj.Hoy;
        var consulta = Visibles().Where(a => a.FechaInicio > hoy);

        if (!string.IsNullOrWhiteSpace(filtro.Q))
        {
            var q = filtro.Q.Trim().ToLowerInvariant();
            consulta = consulta.Where(a => a.Nombre.ToLower().Contains(q) || a.Descripcion.ToLower().Contains(q) ||
                                           a.Destino.ToLower().Contains(q) || a.Empresa.NombreComercial.ToLower().Contains(q));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Destino))
        {
            var destino = filtro.Destino.Trim().ToLowerInvariant();
            consulta = consulta.Where(a => a.Destino.ToLower() == destino);
        }
        if (filtro.Desde is { } desde) consulta = consulta.Where(a => a.FechaInicio >= desde);
        if (filtro.Hasta is { } hasta) consulta = consulta.Where(a => a.FechaInicio <= hasta);
        if (filtro.PrecioMin is { } min) consulta = consulta.Where(a => a.Precio >= min);
        if (filtro.PrecioMax is { } max) consulta = consulta.Where(a => a.Precio <= max);

        var filas = Proyectar(consulta);
        filas = (filtro.Orden ?? "fecha") switch
        {
            "precio-asc" => filas.OrderBy(f => f.Precio).ThenBy(f => f.FechaInicio).ThenBy(f => f.Id),
            "precio-desc" => filas.OrderByDescending(f => f.Precio).ThenBy(f => f.FechaInicio).ThenBy(f => f.Id),
            "valoracion" => filas.OrderByDescending(f => f.Valoracion ?? f.ValoracionEmpresa ?? 0)
                .ThenByDescending(f => f.TotalResenasEmpresa).ThenBy(f => f.FechaInicio).ThenBy(f => f.Id),
            _ => filas.OrderBy(f => f.FechaInicio).ThenBy(f => f.Id),
        };

        var (pagina, tamano) = PaginadoExtensions.Normalizar(filtro.Pagina, filtro.Tamano);
        var resultado = await filas.PaginarAsync(pagina, tamano, ct);
        return new Paginado<ActividadResumenDto>(
            resultado.Items.Select(AResumen).ToList(), resultado.Pagina, resultado.Tamano, resultado.Total);
    }

    public async Task<IReadOnlyList<string>> DestinosAsync(CancellationToken ct = default)
    {
        var hoy = reloj.Hoy;
        return await Visibles().Where(a => a.FechaInicio > hoy)
            .Select(a => a.Destino).Distinct().OrderBy(d => d).ToListAsync(ct);
    }

    public async Task<ActividadDetalleDto> DetalleAsync(int id, CancellationToken ct = default)
    {
        var actividad = await db.Actividades.AsNoTracking()
            .Include(a => a.Empresa)
            .Include(a => a.Itinerario)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (actividad is null || !PuedeVer(actividad))
            throw new NoEncontradoException("La actividad no existe o ya no está disponible.");

        var hoy = reloj.Hoy;
        var (valoracion, total) = await ValoracionActividadAsync(id, ct);
        var (valoracionEmpresa, totalEmpresa) = await ValoracionEmpresaAsync(actividad.EmpresaId, ct);

        return new ActividadDetalleDto(
            actividad.Id, actividad.Nombre, actividad.Descripcion, actividad.Destino, actividad.PuntoEncuentro,
            actividad.FechaInicio, actividad.FechaFin, actividad.DuracionDias, actividad.Precio, actividad.Capacidad,
            actividad.CuposDisponibles, actividad.Incluye, actividad.NoIncluye, actividad.ImagenUrl, actividad.Estado,
            actividad.EsReservable(hoy) && actividad.Empresa.Estado == EstadoEmpresa.Verificada,
            actividad.HaFinalizado(hoy),
            actividad.Itinerario.OrderBy(d => d.Dia).Select(d => new ItinerarioDiaDto(d.Dia, d.Titulo, d.Descripcion)).ToList(),
            valoracion, total,
            new EmpresaPublicaDto(actividad.Empresa.Id, actividad.Empresa.NombreComercial, actividad.Empresa.Descripcion,
                actividad.Empresa.LogoUrl, valoracionEmpresa, totalEmpresa));
    }

    public async Task<ResenasActividadDto> ResenasAsync(int actividadId, int? pagina, CancellationToken ct = default)
    {
        var actividad = await db.Actividades.AsNoTracking().Include(a => a.Empresa)
            .FirstOrDefaultAsync(a => a.Id == actividadId, ct);
        if (actividad is null || !PuedeVer(actividad))
            throw new NoEncontradoException("La actividad no existe o ya no está disponible.");

        var visibles = db.Resenas.AsNoTracking().Where(r => r.ActividadId == actividadId && !r.Oculta);
        var conteo = await visibles.GroupBy(r => r.Valoracion)
            .Select(g => new { Valoracion = g.Key, Cantidad = g.Count() }).ToListAsync(ct);
        var distribucion = Enumerable.Range(1, 5)
            .ToDictionary(v => v, v => conteo.FirstOrDefault(c => c.Valoracion == v)?.Cantidad ?? 0);
        var total = distribucion.Values.Sum();
        double? promedio = total == 0 ? null : Math.Round(distribucion.Sum(d => d.Key * d.Value) / (double)total, 1);

        var (p, tamano) = PaginadoExtensions.Normalizar(pagina, 10);
        var pagina1 = await visibles.OrderByDescending(r => r.CreadoEn)
            .Select(r => new { r.Id, r.Usuario.Nombre, r.Usuario.Apellidos, r.Valoracion, r.Comentario, r.CreadoEn })
            .PaginarAsync(p, tamano, ct);

        return new ResenasActividadDto(promedio, total, distribucion,
            pagina1.Items.Select(r => new ResenaDto(r.Id, NombrePublico(r.Nombre, r.Apellidos), r.Valoracion, r.Comentario, r.CreadoEn)).ToList(),
            pagina1.Pagina, pagina1.TotalPaginas);
    }

    // ---------- Auxiliares ----------

    private bool PuedeVer(Actividad a)
    {
        if (a.Estado == EstadoActividad.Publicada && a.Empresa.Estado == EstadoEmpresa.Verificada) return true;
        // Vista previa para la empresa dueña y para administradores
        return usuarioActual.Rol == Rol.Admin || (usuarioActual.Id is { } id && a.Empresa.UsuarioId == id);
    }

    private Task<(double?, int)> ValoracionActividadAsync(int actividadId, CancellationToken ct) =>
        ValorarAsync(db.Resenas.Where(r => r.ActividadId == actividadId && !r.Oculta), ct);

    private Task<(double?, int)> ValoracionEmpresaAsync(int empresaId, CancellationToken ct) =>
        ValorarAsync(db.Resenas.Where(r => r.Actividad.EmpresaId == empresaId && !r.Oculta), ct);

    private static async Task<(double?, int)> ValorarAsync(IQueryable<Resena> resenas, CancellationToken ct)
    {
        var total = await resenas.CountAsync(ct);
        if (total == 0) return (null, 0);
        var promedio = await resenas.AverageAsync(r => (double)r.Valoracion, ct);
        return (Math.Round(promedio, 1), total);
    }

    /// <summary>"Ana Quispe Mamani" → "Ana Q." (privacidad del autor de la reseña).</summary>
    internal static string NombrePublico(string nombre, string apellidos)
    {
        var primerNombre = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nombre;
        return string.IsNullOrWhiteSpace(apellidos) ? primerNombre : $"{primerNombre} {char.ToUpperInvariant(apellidos.Trim()[0])}.";
    }

    private static void ValidarFiltro(FiltroCatalogo f)
    {
        var errores = new Dictionary<string, string[]>();
        if (f.Desde is { } d && f.Hasta is { } h && d > h)
            errores["hasta"] = ["La fecha final debe ser igual o posterior a la inicial."];
        if (f.PrecioMin is { } min && f.PrecioMax is { } max && min > max)
            errores["precioMax"] = ["El precio máximo debe ser mayor o igual al mínimo."];
        if (f.PrecioMin < 0) errores["precioMin"] = ["El precio no puede ser negativo."];
        if (f.Orden is not null && !Ordenes.Contains(f.Orden))
            errores["orden"] = [$"Orden no válido. Valores permitidos: {string.Join(", ", Ordenes)}."];
        if (f.Q?.Length > 100) errores["q"] = ["La búsqueda no puede superar los 100 caracteres."];
        if (errores.Count > 0) throw new ValidacionException(errores);
    }

    /// <summary>Proyección intermedia. Usa inicializadores (no constructor) para que EF pueda ordenar por sus miembros.</summary>
    private sealed class Fila
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = "";
        public string Destino { get; init; } = "";
        public DateOnly FechaInicio { get; init; }
        public DateOnly FechaFin { get; init; }
        public decimal Precio { get; init; }
        public int Capacidad { get; init; }
        public int CuposReservados { get; init; }
        public string? ImagenUrl { get; init; }
        public double? Valoracion { get; init; }
        public int TotalResenas { get; init; }
        public int EmpresaId { get; init; }
        public string EmpresaNombre { get; init; } = "";
        public double? ValoracionEmpresa { get; init; }
        public int TotalResenasEmpresa { get; init; }
    }

    private static IQueryable<Fila> Proyectar(IQueryable<Actividad> consulta) => consulta.Select(a => new Fila
    {
        Id = a.Id,
        Nombre = a.Nombre,
        Destino = a.Destino,
        FechaInicio = a.FechaInicio,
        FechaFin = a.FechaFin,
        Precio = a.Precio,
        Capacidad = a.Capacidad,
        CuposReservados = a.CuposReservados,
        ImagenUrl = a.ImagenUrl,
        Valoracion = a.Resenas.Where(r => !r.Oculta).Average(r => (double?)r.Valoracion),
        TotalResenas = a.Resenas.Count(r => !r.Oculta),
        EmpresaId = a.EmpresaId,
        EmpresaNombre = a.Empresa.NombreComercial,
        ValoracionEmpresa = a.Empresa.Actividades.SelectMany(x => x.Resenas).Where(r => !r.Oculta).Average(r => (double?)r.Valoracion),
        TotalResenasEmpresa = a.Empresa.Actividades.SelectMany(x => x.Resenas).Count(r => !r.Oculta),
    });

    private static ActividadResumenDto AResumen(Fila f) => new(
        f.Id, f.Nombre, f.Destino, f.FechaInicio, f.FechaFin, f.FechaFin.DayNumber - f.FechaInicio.DayNumber + 1,
        f.Precio, f.Capacidad, Math.Max(0, f.Capacidad - f.CuposReservados), f.ImagenUrl,
        Redondear(f.Valoracion), f.TotalResenas,
        new EmpresaResumenDto(f.EmpresaId, f.EmpresaNombre, Redondear(f.ValoracionEmpresa), f.TotalResenasEmpresa));

    private static double? Redondear(double? valor) => valor is null ? null : Math.Round(valor.Value, 1);
}
