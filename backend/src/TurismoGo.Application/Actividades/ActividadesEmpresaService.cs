using System.Globalization;
using System.Text;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Application.Comun;
using TurismoGo.Domain;
using TurismoGo.Domain.Entidades;

namespace TurismoGo.Application.Actividades;

/// <summary>Gestión de actividades por parte de la empresa dueña (RQ-003).</summary>
public interface IActividadesEmpresaService
{
    Task<Paginado<ActividadEmpresaFilaDto>> ListarAsync(string? q, EstadoActividad? estado, int? pagina, int? tamano, CancellationToken ct = default);
    Task<ActividadFormularioDto> ObtenerAsync(int id, CancellationToken ct = default);
    Task<int> CrearAsync(GuardarActividadRequest solicitud, CancellationToken ct = default);
    Task ActualizarAsync(int id, GuardarActividadRequest solicitud, CancellationToken ct = default);
    Task PublicarAsync(int id, CancellationToken ct = default);
    Task DespublicarAsync(int id, CancellationToken ct = default);
    Task CancelarAsync(int id, string? motivo, CancellationToken ct = default);
    Task<int> DuplicarAsync(int id, CancellationToken ct = default);
    Task EliminarAsync(int id, CancellationToken ct = default);
    Task<PanelEmpresaDto> PanelAsync(CancellationToken ct = default);
    Task<ArchivoDto> ParticipantesCsvAsync(int id, CancellationToken ct = default);
}

internal sealed class ActividadesEmpresaService(
    IAppDbContext db,
    IReloj reloj,
    IUsuarioActual usuarioActual,
    IValidator<GuardarActividadRequest> validador) : IActividadesEmpresaService
{
    public async Task<Paginado<ActividadEmpresaFilaDto>> ListarAsync(
        string? q, EstadoActividad? estado, int? pagina, int? tamano, CancellationToken ct = default)
    {
        var empresa = await EmpresaActualAsync(ct);
        var hoy = reloj.Hoy;
        var consulta = db.Actividades.AsNoTracking().Where(a => a.EmpresaId == empresa.Id);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var texto = q.Trim().ToLowerInvariant();
            consulta = consulta.Where(a => a.Nombre.ToLower().Contains(texto) || a.Destino.ToLower().Contains(texto));
        }
        if (estado is not null) consulta = consulta.Where(a => a.Estado == estado);

        var (p, t) = PaginadoExtensions.Normalizar(pagina, tamano, 20);
        return await consulta
            .OrderBy(a => a.FechaFin < hoy).ThenBy(a => a.FechaInicio).ThenBy(a => a.Id)
            .Select(a => new ActividadEmpresaFilaDto(
                a.Id, a.Nombre, a.Destino, a.FechaInicio, a.FechaFin, a.Precio, a.Capacidad, a.CuposReservados,
                a.Estado, a.FechaFin < hoy, a.Reservas.Count(r => r.Estado == EstadoReserva.Pendiente), a.ImagenUrl))
            .PaginarAsync(p, t, ct);
    }

    public async Task<ActividadFormularioDto> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var a = await ActividadPropiaAsync(id, ct, conItinerario: true);
        var activas = await TieneReservasActivasAsync(id, ct);
        return new ActividadFormularioDto(
            a.Id, a.Nombre, a.Descripcion, a.Destino, a.PuntoEncuentro, a.FechaInicio, a.FechaFin, a.Precio, a.Capacidad,
            a.CuposReservados, a.Incluye, a.NoIncluye, a.ImagenUrl, a.Estado, activas, a.HaFinalizado(reloj.Hoy),
            a.Itinerario.OrderBy(d => d.Dia).Select(d => new ItinerarioDiaDto(d.Dia, d.Titulo, d.Descripcion)).ToList());
    }

    public async Task<int> CrearAsync(GuardarActividadRequest solicitud, CancellationToken ct = default)
    {
        await validador.ValidarYLanzarAsync(solicitud, ct);
        var empresa = await EmpresaActualAsync(ct);
        if (empresa.Estado == EstadoEmpresa.Suspendida)
            throw new ProhibidoException("Tu empresa está suspendida y no puede crear actividades.");

        var actividad = new Actividad { EmpresaId = empresa.Id, Estado = EstadoActividad.Borrador };
        Aplicar(actividad, solicitud);
        db.Actividades.Add(actividad);
        await db.SaveChangesAsync(ct);
        return actividad.Id;
    }

    public async Task ActualizarAsync(int id, GuardarActividadRequest solicitud, CancellationToken ct = default)
    {
        var actividad = await ActividadPropiaAsync(id, ct, conItinerario: true);
        var hoy = reloj.Hoy;
        if (actividad.Estado == EstadoActividad.Cancelada)
            throw new ReglaNegocioException("Una actividad cancelada no se puede editar. Puedes duplicarla.");
        if (actividad.FechaInicio <= hoy)
            throw new ReglaNegocioException("La actividad ya comenzó o finalizó; no se puede editar.");

        await validador.ValidarYLanzarAsync(solicitud, ct);

        if (await TieneReservasActivasAsync(id, ct))
        {
            if (solicitud.FechaInicio != actividad.FechaInicio || solicitud.FechaFin != actividad.FechaFin)
                throw new ReglaNegocioException(
                    "La actividad tiene reservas activas: no puedes cambiar sus fechas. Cancélala y crea una nueva salida si es necesario.");
        }
        if (solicitud.Capacidad < actividad.CuposReservados)
            throw ValidacionException.De("capacidad",
                $"La capacidad no puede ser menor que los {actividad.CuposReservados} cupos ya reservados.");
        if (actividad.Estado == EstadoActividad.Publicada && (solicitud.Itinerario is null || solicitud.Itinerario.Count == 0))
            throw ValidacionException.De("itinerario", "Una actividad publicada debe tener al menos un día de itinerario.");

        db.ItinerarioDias.RemoveRange(actividad.Itinerario);
        actividad.Itinerario.Clear();
        Aplicar(actividad, solicitud);
        actividad.Version = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
    }

    public async Task PublicarAsync(int id, CancellationToken ct = default)
    {
        var empresa = await EmpresaActualAsync(ct);
        if (!empresa.PuedePublicar)
            throw new ReglaNegocioException(empresa.Estado == EstadoEmpresa.Suspendida
                ? "Tu empresa está suspendida y no puede publicar actividades."
                : "Tu empresa aún no ha sido verificada por TurismoGo. Podrás publicar cuando se complete la verificación.");

        var actividad = await ActividadPropiaAsync(id, ct, conItinerario: true);
        actividad.Publicar(reloj.Hoy);
        await db.SaveChangesAsync(ct);
    }

    public async Task DespublicarAsync(int id, CancellationToken ct = default)
    {
        var actividad = await ActividadPropiaAsync(id, ct);
        if (actividad.Estado != EstadoActividad.Publicada)
            throw new ReglaNegocioException("Solo se pueden despublicar actividades publicadas.");
        if (await TieneReservasActivasAsync(id, ct))
            throw new ReglaNegocioException(
                "La actividad tiene reservas activas. Si no se realizará, cancélala para avisar a los clientes.");
        actividad.Estado = EstadoActividad.Borrador;
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelarAsync(int id, string? motivo, CancellationToken ct = default)
    {
        var motivoLimpio = Validacion.Limpiar(motivo);
        if (motivoLimpio is null || motivoLimpio.Length is < 5 or > 300)
            throw ValidacionException.De("motivo", "Indica el motivo de la cancelación (entre 5 y 300 caracteres).");

        var actividad = await ActividadPropiaAsync(id, ct);
        if (actividad.Estado == EstadoActividad.Cancelada)
            throw new ReglaNegocioException("La actividad ya está cancelada.");
        if (actividad.HaFinalizado(reloj.Hoy))
            throw new ReglaNegocioException("La actividad ya finalizó.");

        var reservas = await db.Reservas
            .Where(r => r.ActividadId == id && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada))
            .ToListAsync(ct);
        foreach (var reserva in reservas)
        {
            reserva.Actividad = actividad;
            reserva.Cancelar(reloj.UtcAhora, $"Actividad cancelada por la empresa: {motivoLimpio}");
        }
        actividad.Estado = EstadoActividad.Cancelada;
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> DuplicarAsync(int id, CancellationToken ct = default)
    {
        var original = await ActividadPropiaAsync(id, ct, conItinerario: true);
        var hoy = reloj.Hoy;
        var duracion = original.DuracionDias;
        var inicio = original.FechaInicio > hoy ? original.FechaInicio : hoy.AddDays(7);

        var nombre = original.Nombre.EndsWith("(copia)", StringComparison.Ordinal) ? original.Nombre : $"{original.Nombre} (copia)";
        var copia = new Actividad
        {
            EmpresaId = original.EmpresaId,
            Nombre = nombre.Length > 100 ? nombre[..100] : nombre,
            Descripcion = original.Descripcion,
            Destino = original.Destino,
            PuntoEncuentro = original.PuntoEncuentro,
            FechaInicio = inicio,
            FechaFin = inicio.AddDays(duracion - 1),
            Precio = original.Precio,
            Capacidad = original.Capacidad,
            Incluye = original.Incluye,
            NoIncluye = original.NoIncluye,
            ImagenUrl = original.ImagenUrl,
            Estado = EstadoActividad.Borrador,
            Itinerario = original.Itinerario
                .Select(d => new ItinerarioDia { Dia = d.Dia, Titulo = d.Titulo, Descripcion = d.Descripcion }).ToList(),
        };
        db.Actividades.Add(copia);
        await db.SaveChangesAsync(ct);
        return copia.Id;
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var actividad = await ActividadPropiaAsync(id, ct, conItinerario: true);
        if (actividad.Estado != EstadoActividad.Borrador)
            throw new ReglaNegocioException("Solo se pueden eliminar borradores. Las actividades publicadas se cancelan.");
        if (await db.Reservas.AnyAsync(r => r.ActividadId == id, ct))
            throw new ConflictoException("La actividad tiene historial de reservas y no puede eliminarse.");
        db.Actividades.Remove(actividad);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PanelEmpresaDto> PanelAsync(CancellationToken ct = default)
    {
        var empresa = await EmpresaActualAsync(ct);
        var hoy = reloj.Hoy;
        // Inicio del mes en hora de Perú (UTC-5) expresado en UTC
        var inicioMesUtc = new DateTime(hoy.Year, hoy.Month, 1, 5, 0, 0, DateTimeKind.Utc);

        var actividades = db.Actividades.AsNoTracking().Where(a => a.EmpresaId == empresa.Id);
        var reservas = db.Reservas.AsNoTracking().Where(r => r.Actividad.EmpresaId == empresa.Id);

        var publicadas = await actividades.CountAsync(a => a.Estado == EstadoActividad.Publicada && a.FechaFin >= hoy, ct);
        var borradores = await actividades.CountAsync(a => a.Estado == EstadoActividad.Borrador, ct);
        var pendientes = await reservas.CountAsync(r => r.Estado == EstadoReserva.Pendiente, ct);

        var delMes = reservas.Where(r => r.CreadoEn >= inicioMesUtc &&
                                         (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada));
        var reservasMes = await delMes.CountAsync(ct);
        var personasMes = await delMes.SumAsync(r => (int?)r.CantidadPersonas, ct) ?? 0;
        var ingresosMes = await delMes.Where(r => r.Estado == EstadoReserva.Confirmada).SumAsync(r => (decimal?)r.Total, ct) ?? 0;

        var resenas = db.Resenas.AsNoTracking().Where(r => r.Actividad.EmpresaId == empresa.Id && !r.Oculta);
        var totalResenas = await resenas.CountAsync(ct);
        double? valoracion = totalResenas == 0 ? null : Math.Round(await resenas.AverageAsync(r => (double)r.Valoracion, ct), 1);

        var proximas = await actividades
            .Where(a => a.Estado == EstadoActividad.Publicada && a.FechaInicio >= hoy)
            .OrderBy(a => a.FechaInicio).Take(5)
            .Select(a => new ProximaSalidaDto(a.Id, a.Nombre, a.FechaInicio, a.Capacidad, a.CuposReservados,
                a.Reservas.Count(r => r.Estado == EstadoReserva.Pendiente)))
            .ToListAsync(ct);

        return new PanelEmpresaDto(empresa.Estado, publicadas, borradores, pendientes, reservasMes, personasMes,
            ingresosMes, valoracion, totalResenas, proximas);
    }

    public async Task<ArchivoDto> ParticipantesCsvAsync(int id, CancellationToken ct = default)
    {
        var actividad = await ActividadPropiaAsync(id, ct);
        var filas = await db.Reservas.AsNoTracking()
            .Where(r => r.ActividadId == id && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada))
            .OrderBy(r => r.Usuario.Apellidos).ThenBy(r => r.Usuario.Nombre)
            .Select(r => new
            {
                r.Codigo, r.Usuario.Apellidos, r.Usuario.Nombre, r.Usuario.Dni, r.Usuario.Correo, r.Usuario.Telefono,
                r.CantidadPersonas, r.Estado, r.Total, r.Notas,
            })
            .ToListAsync(ct);

        var csv = new StringBuilder();
        // Separador ";" para que Excel en español abra el archivo en columnas
        csv.AppendLine("Código;Apellidos;Nombres;DNI;Correo;Teléfono;Personas;Estado;Total (S/);Notas");
        foreach (var f in filas)
        {
            csv.AppendLine(string.Join(';',
                Celda(f.Codigo), Celda(f.Apellidos), Celda(f.Nombre), Celda(f.Dni), Celda(f.Correo), Celda(f.Telefono),
                f.CantidadPersonas.ToString(CultureInfo.InvariantCulture), Celda(f.Estado.ToString()),
                f.Total.ToString("0.00", CultureInfo.InvariantCulture), Celda(f.Notas)));
        }
        csv.AppendLine();
        csv.AppendLine(string.Join(';', "Total personas", "", "", "", "", "",
            filas.Sum(f => f.CantidadPersonas).ToString(CultureInfo.InvariantCulture)));

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        var nombre = $"participantes-{actividad.FechaInicio:yyyy-MM-dd}-{Slug(actividad.Nombre)}.csv";
        return new ArchivoDto(bytes, "text/csv; charset=utf-8", nombre);
    }

    // ---------- Auxiliares ----------

    private async Task<Empresa> EmpresaActualAsync(CancellationToken ct)
    {
        var usuarioId = usuarioActual.IdRequerido;
        return await db.Empresas.FirstOrDefaultAsync(e => e.UsuarioId == usuarioId, ct)
               ?? throw new ProhibidoException("Tu cuenta no tiene una empresa asociada.");
    }

    private async Task<Actividad> ActividadPropiaAsync(int id, CancellationToken ct, bool conItinerario = false)
    {
        var usuarioId = usuarioActual.IdRequerido;
        IQueryable<Actividad> consulta = db.Actividades;
        if (conItinerario) consulta = consulta.Include(a => a.Itinerario);
        // Si la actividad es de otra empresa se responde 404: no se revela su existencia.
        return await consulta.FirstOrDefaultAsync(a => a.Id == id && a.Empresa.UsuarioId == usuarioId, ct)
               ?? throw new NoEncontradoException("La actividad no existe.");
    }

    private Task<bool> TieneReservasActivasAsync(int actividadId, CancellationToken ct) =>
        db.Reservas.AnyAsync(r => r.ActividadId == actividadId &&
                                  (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada), ct);

    private static void Aplicar(Actividad a, GuardarActividadRequest s)
    {
        a.Nombre = s.Nombre.Trim();
        a.Descripcion = s.Descripcion.Trim();
        a.Destino = s.Destino.Trim();
        a.PuntoEncuentro = s.PuntoEncuentro.Trim();
        a.FechaInicio = s.FechaInicio;
        a.FechaFin = s.FechaFin;
        a.Precio = decimal.Round(s.Precio, 2);
        a.Capacidad = s.Capacidad;
        a.Incluye = Validacion.Limpiar(s.Incluye);
        a.NoIncluye = Validacion.Limpiar(s.NoIncluye);
        a.ImagenUrl = Validacion.Limpiar(s.ImagenUrl);
        a.Itinerario = (s.Itinerario ?? []).OrderBy(d => d.Dia)
            .Select(d => new ItinerarioDia { Dia = d.Dia, Titulo = d.Titulo.Trim(), Descripcion = d.Descripcion.Trim() })
            .ToList();
    }

    /// <summary>Escapa una celda CSV y neutraliza fórmulas (inyección CSV en Excel).</summary>
    internal static string Celda(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return "";
        var v = valor;
        if (v[0] is '=' or '+' or '-' or '@' or '\t' or '\r') v = "'" + v;
        return v.IndexOfAny([';', '"', '\n', '\r']) >= 0 ? $"\"{v.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : v;
    }

    private static string Slug(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
        }
        var slug = string.Join('-', sb.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        return slug.Length > 40 ? slug[..40] : slug;
    }
}
