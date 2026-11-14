using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Application.Comun;
using TurismoGo.Domain;

namespace TurismoGo.Application.Administracion;

public sealed record MesReservasDto(string Mes, int Reservas, decimal MontoConfirmado);
public sealed record DestinoTopDto(string Destino, int Reservas, int Personas);
public sealed record EmpresaTopDto(int Id, string NombreComercial, double? Valoracion, int TotalResenas, int Reservas);

public sealed record AdminResumenDto(
    int Turistas, int Empresas, int EmpresasPendientes, int ActividadesPublicadas, int ReservasActivas,
    int ResenasOcultas, IReadOnlyList<MesReservasDto> ReservasPorMes, IReadOnlyList<DestinoTopDto> TopDestinos,
    IReadOnlyList<EmpresaTopDto> TopEmpresas);

public sealed record EmpresaAdminDto(
    int Id, string NombreComercial, string RazonSocial, string Ruc, string Representante, string Correo, string Telefono,
    string Direccion, EstadoEmpresa Estado, string? MotivoEstado, DateTime CreadoEn, DateTime? VerificadaEn, int ActividadesPublicadas);

public sealed record UsuarioAdminDto(
    int Id, string Nombre, string Correo, Rol Rol, bool Activo, string? Dni, string? Empresa, DateTime CreadoEn, DateTime? UltimoAccesoEn);

public sealed record ResenaAdminDto(
    int Id, string Autor, string Correo, int ActividadId, string Actividad, string Empresa, int Valoracion,
    string Comentario, bool Oculta, string? MotivoOcultamiento, DateTime CreadoEn);

public interface IAdminService
{
    Task<AdminResumenDto> ResumenAsync(CancellationToken ct = default);
    Task<Paginado<EmpresaAdminDto>> EmpresasAsync(EstadoEmpresa? estado, string? q, int? pagina, CancellationToken ct = default);
    Task VerificarEmpresaAsync(int id, CancellationToken ct = default);
    Task SuspenderEmpresaAsync(int id, string? motivo, CancellationToken ct = default);
    Task<Paginado<UsuarioAdminDto>> UsuariosAsync(Rol? rol, string? q, int? pagina, CancellationToken ct = default);
    Task CambiarEstadoUsuarioAsync(int id, bool activo, CancellationToken ct = default);
    Task<Paginado<ResenaAdminDto>> ResenasAsync(bool? oculta, string? q, int? pagina, CancellationToken ct = default);
    Task OcultarResenaAsync(int id, string? motivo, CancellationToken ct = default);
    Task MostrarResenaAsync(int id, CancellationToken ct = default);
}

internal sealed class AdminService(IAppDbContext db, IReloj reloj, IUsuarioActual usuarioActual) : IAdminService
{
    private const int MesesReporte = 6;

    public async Task<AdminResumenDto> ResumenAsync(CancellationToken ct = default)
    {
        var hoy = reloj.Hoy;
        var turistas = await db.Usuarios.CountAsync(u => u.Rol == Rol.Turista, ct);
        var empresas = await db.Empresas.CountAsync(ct);
        var pendientes = await db.Empresas.CountAsync(e => e.Estado == EstadoEmpresa.Pendiente, ct);
        var publicadas = await db.Actividades.CountAsync(a => a.Estado == EstadoActividad.Publicada && a.FechaFin >= hoy, ct);
        var reservasActivas = await db.Reservas.CountAsync(
            r => (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada) && r.Actividad.FechaFin >= hoy, ct);
        var ocultas = await db.Resenas.CountAsync(r => r.Oculta, ct);

        // Reservas por mes de creación (últimos 6 meses, hora de Perú aproximada como UTC-5)
        var primerMes = new DateTime(hoy.Year, hoy.Month, 1, 5, 0, 0, DateTimeKind.Utc).AddMonths(-(MesesReporte - 1));
        var porMes = await db.Reservas
            .Where(r => r.CreadoEn >= primerMes && r.Estado != EstadoReserva.Rechazada)
            .GroupBy(r => new { r.CreadoEn.Year, r.CreadoEn.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Reservas = g.Count(r => r.Estado != EstadoReserva.Cancelada),
                Monto = g.Where(r => r.Estado == EstadoReserva.Confirmada).Sum(r => (decimal?)r.Total) ?? 0,
            })
            .ToListAsync(ct);
        var meses = Enumerable.Range(0, MesesReporte).Select(i => primerMes.AddMonths(i)).Select(m =>
        {
            var dato = porMes.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);
            return new MesReservasDto(m.ToString("yyyy-MM", CultureInfo.InvariantCulture), dato?.Reservas ?? 0, dato?.Monto ?? 0);
        }).ToList();

        var topDestinos = await db.Reservas
            .Where(r => r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada)
            .GroupBy(r => r.Actividad.Destino)
            .Select(g => new { Destino = g.Key, Reservas = g.Count(), Personas = g.Sum(r => r.CantidadPersonas) })
            .OrderByDescending(d => d.Reservas).ThenBy(d => d.Destino).Take(5)
            .ToListAsync(ct);

        var topEmpresas = await db.Empresas.Where(e => e.Estado == EstadoEmpresa.Verificada)
            .Select(e => new
            {
                e.Id,
                e.NombreComercial,
                Valoracion = e.Actividades.SelectMany(a => a.Resenas).Where(r => !r.Oculta).Average(r => (double?)r.Valoracion),
                TotalResenas = e.Actividades.SelectMany(a => a.Resenas).Count(r => !r.Oculta),
                Reservas = e.Actividades.SelectMany(a => a.Reservas)
                    .Count(r => r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada),
            })
            .OrderByDescending(e => e.Reservas).ThenByDescending(e => e.Valoracion).Take(5)
            .ToListAsync(ct);

        return new AdminResumenDto(turistas, empresas, pendientes, publicadas, reservasActivas, ocultas, meses,
            topDestinos.Select(d => new DestinoTopDto(d.Destino, d.Reservas, d.Personas)).ToList(),
            topEmpresas.Select(e => new EmpresaTopDto(e.Id, e.NombreComercial,
                e.Valoracion is null ? null : Math.Round(e.Valoracion.Value, 1), e.TotalResenas, e.Reservas)).ToList());
    }

    // ---------- Empresas ----------

    public async Task<Paginado<EmpresaAdminDto>> EmpresasAsync(EstadoEmpresa? estado, string? q, int? pagina, CancellationToken ct = default)
    {
        var hoy = reloj.Hoy;
        var consulta = db.Empresas.AsNoTracking();
        if (estado is not null) consulta = consulta.Where(e => e.Estado == estado);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim().ToLowerInvariant();
            consulta = consulta.Where(e => e.NombreComercial.ToLower().Contains(t) || e.RazonSocial.ToLower().Contains(t) ||
                                           e.Ruc.Contains(t) || e.Usuario.Correo.Contains(t));
        }
        var (p, tamano) = PaginadoExtensions.Normalizar(pagina, 20);
        return await consulta
            .OrderBy(e => e.Estado == EstadoEmpresa.Pendiente ? 0 : 1).ThenByDescending(e => e.CreadoEn)
            .Select(e => new EmpresaAdminDto(
                e.Id, e.NombreComercial, e.RazonSocial, e.Ruc, e.Usuario.Nombre + " " + e.Usuario.Apellidos, e.Usuario.Correo,
                e.Telefono, e.Direccion, e.Estado, e.MotivoEstado, e.CreadoEn, e.VerificadaEn,
                e.Actividades.Count(a => a.Estado == EstadoActividad.Publicada && a.FechaFin >= hoy)))
            .PaginarAsync(p, tamano, ct);
    }

    public async Task VerificarEmpresaAsync(int id, CancellationToken ct = default)
    {
        var empresa = await db.Empresas.FirstOrDefaultAsync(e => e.Id == id, ct)
                      ?? throw new NoEncontradoException("La empresa no existe.");
        if (empresa.Estado == EstadoEmpresa.Verificada)
            throw new ReglaNegocioException("La empresa ya está verificada.");
        empresa.Verificar(reloj.UtcAhora);
        await db.SaveChangesAsync(ct);
    }

    public async Task SuspenderEmpresaAsync(int id, string? motivo, CancellationToken ct = default)
    {
        var m = ValidarMotivo(motivo);
        var empresa = await db.Empresas.FirstOrDefaultAsync(e => e.Id == id, ct)
                      ?? throw new NoEncontradoException("La empresa no existe.");
        if (empresa.Estado == EstadoEmpresa.Suspendida)
            throw new ReglaNegocioException("La empresa ya está suspendida.");
        empresa.Suspender(m);
        await db.SaveChangesAsync(ct);
    }

    // ---------- Usuarios ----------

    public async Task<Paginado<UsuarioAdminDto>> UsuariosAsync(Rol? rol, string? q, int? pagina, CancellationToken ct = default)
    {
        var consulta = db.Usuarios.AsNoTracking();
        if (rol is not null) consulta = consulta.Where(u => u.Rol == rol);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim().ToLowerInvariant();
            consulta = consulta.Where(u => u.Nombre.ToLower().Contains(t) || u.Apellidos.ToLower().Contains(t) ||
                                           u.Correo.Contains(t) || (u.Dni != null && u.Dni.Contains(t)));
        }
        var (p, tamano) = PaginadoExtensions.Normalizar(pagina, 20);
        return await consulta.OrderByDescending(u => u.CreadoEn).ThenByDescending(u => u.Id)
            .Select(u => new UsuarioAdminDto(u.Id, u.Nombre + " " + u.Apellidos, u.Correo, u.Rol, u.Activo, u.Dni,
                u.Empresa != null ? u.Empresa.NombreComercial : null, u.CreadoEn, u.UltimoAccesoEn))
            .PaginarAsync(p, tamano, ct);
    }

    public async Task CambiarEstadoUsuarioAsync(int id, bool activo, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct)
                      ?? throw new NoEncontradoException("El usuario no existe.");
        if (usuario.Id == usuarioActual.IdRequerido)
            throw new ReglaNegocioException("No puedes suspender tu propia cuenta.");
        if (usuario.Rol == Rol.Admin)
            throw new ReglaNegocioException("No se puede suspender a otro administrador desde el panel.");
        if (usuario.Activo == activo)
            throw new ReglaNegocioException(activo ? "La cuenta ya está activa." : "La cuenta ya está suspendida.");
        usuario.Activo = activo;
        await db.SaveChangesAsync(ct);
    }

    // ---------- Reseñas ----------

    public async Task<Paginado<ResenaAdminDto>> ResenasAsync(bool? oculta, string? q, int? pagina, CancellationToken ct = default)
    {
        var consulta = db.Resenas.AsNoTracking();
        if (oculta is not null) consulta = consulta.Where(r => r.Oculta == oculta);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim().ToLowerInvariant();
            consulta = consulta.Where(r => r.Comentario.ToLower().Contains(t) || r.Actividad.Nombre.ToLower().Contains(t) ||
                                           r.Usuario.Correo.Contains(t));
        }
        var (p, tamano) = PaginadoExtensions.Normalizar(pagina, 20);
        return await consulta.OrderBy(r => r.Valoracion).ThenByDescending(r => r.CreadoEn)
            .Select(r => new ResenaAdminDto(r.Id, r.Usuario.Nombre + " " + r.Usuario.Apellidos, r.Usuario.Correo,
                r.ActividadId, r.Actividad.Nombre, r.Actividad.Empresa.NombreComercial, r.Valoracion, r.Comentario,
                r.Oculta, r.MotivoOcultamiento, r.CreadoEn))
            .PaginarAsync(p, tamano, ct);
    }

    public async Task OcultarResenaAsync(int id, string? motivo, CancellationToken ct = default)
    {
        var m = ValidarMotivo(motivo);
        var resena = await db.Resenas.FirstOrDefaultAsync(r => r.Id == id, ct)
                     ?? throw new NoEncontradoException("La reseña no existe.");
        resena.Oculta = true;
        resena.MotivoOcultamiento = m;
        await db.SaveChangesAsync(ct);
    }

    public async Task MostrarResenaAsync(int id, CancellationToken ct = default)
    {
        var resena = await db.Resenas.FirstOrDefaultAsync(r => r.Id == id, ct)
                     ?? throw new NoEncontradoException("La reseña no existe.");
        resena.Oculta = false;
        resena.MotivoOcultamiento = null;
        await db.SaveChangesAsync(ct);
    }

    private static string ValidarMotivo(string? motivo)
    {
        var m = Validacion.Limpiar(motivo);
        if (m is null || m.Length is < 5 or > 300)
            throw ValidacionException.De("motivo", "Indica el motivo (entre 5 y 300 caracteres).");
        return m;
    }
}
