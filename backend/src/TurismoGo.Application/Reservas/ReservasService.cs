using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Application.Comun;
using TurismoGo.Domain;
using TurismoGo.Domain.Entidades;

namespace TurismoGo.Application.Reservas;

public interface IReservasService
{
    // Turista
    Task<ReservaTuristaDto> CrearAsync(CrearReservaRequest solicitud, CancellationToken ct = default);
    Task<Paginado<ReservaTuristaDto>> MisReservasAsync(string? filtro, int? pagina, CancellationToken ct = default);
    Task<ReservaTuristaDto> CancelarAsync(int id, string? motivo, CancellationToken ct = default);
    Task CrearResenaAsync(int actividadId, CrearResenaRequest solicitud, CancellationToken ct = default);

    // Empresa
    Task<Paginado<ReservaEmpresaDto>> RecibidasAsync(int? actividadId, EstadoReserva? estado, string? q, int? pagina, CancellationToken ct = default);
    Task ConfirmarAsync(int id, CancellationToken ct = default);
    Task RechazarAsync(int id, string? motivo, CancellationToken ct = default);
}

public sealed class CrearReservaValidador : AbstractValidator<CrearReservaRequest>
{
    public CrearReservaValidador(IOptions<ReglasNegocioOptions> reglas)
    {
        var max = reglas.Value.MaxPersonasPorReserva;
        RuleFor(x => x.ActividadId).GreaterThan(0).WithMessage("Selecciona una actividad.");
        RuleFor(x => x.CantidadPersonas).InclusiveBetween(1, max)
            .WithMessage($"La cantidad de personas debe estar entre 1 y {max}.");
        RuleFor(x => x.Notas).MaximumLength(500).WithMessage("Las notas no pueden superar los 500 caracteres.");
    }
}

public sealed class CrearResenaValidador : AbstractValidator<CrearResenaRequest>
{
    public CrearResenaValidador()
    {
        RuleFor(x => x.Valoracion).InclusiveBetween(1, 5).WithMessage("Selecciona una valoración de 1 a 5 estrellas.");
        RuleFor(x => x.Comentario).NotEmpty().WithMessage("Escribe un comentario.")
            .MinimumLength(10).WithMessage("El comentario debe tener al menos 10 caracteres.")
            .MaximumLength(1000).WithMessage("El comentario no puede superar los 1000 caracteres.");
    }
}

internal sealed class ReservasService(
    IAppDbContext db,
    IReloj reloj,
    IUsuarioActual usuarioActual,
    IOptions<ReglasNegocioOptions> reglas,
    IValidator<CrearReservaRequest> validadorReserva,
    IValidator<CrearResenaRequest> validadorResena) : IReservasService
{
    private const int ReintentosConcurrencia = 3;
    private int DiasCancelacion => reglas.Value.DiasMinimosCancelacion;

    // ================= Turista =================

    public async Task<ReservaTuristaDto> CrearAsync(CrearReservaRequest solicitud, CancellationToken ct = default)
    {
        await validadorReserva.ValidarYLanzarAsync(solicitud, ct);
        var usuarioId = usuarioActual.IdRequerido;

        for (var intento = 1; ; intento++)
        {
            var actividad = await db.Actividades.Include(a => a.Empresa)
                .FirstOrDefaultAsync(a => a.Id == solicitud.ActividadId, ct);
            if (actividad is null || actividad.Estado != EstadoActividad.Publicada ||
                actividad.Empresa.Estado != EstadoEmpresa.Verificada)
                throw new NoEncontradoException("La actividad no existe o ya no está disponible.");

            if (await db.Reservas.AnyAsync(r => r.UsuarioId == usuarioId && r.ActividadId == actividad.Id &&
                    (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada), ct))
                throw new ConflictoException("Ya tienes una reserva activa para esta actividad. Revísala en \"Mis reservas\".");

            actividad.ReservarCupos(solicitud.CantidadPersonas, reloj.Hoy);
            var reserva = new Reserva
            {
                Codigo = CodigoReserva.Generar(),
                UsuarioId = usuarioId,
                Actividad = actividad,
                CantidadPersonas = solicitud.CantidadPersonas,
                PrecioUnitario = actividad.Precio,
                Total = actividad.Precio * solicitud.CantidadPersonas,
                Estado = EstadoReserva.Pendiente,
                Notas = Validacion.Limpiar(solicitud.Notas),
            };
            db.Reservas.Add(reserva);

            try
            {
                await db.SaveChangesAsync(ct);
                return ATurista(reserva, actividad, resenada: false);
            }
            catch (DbUpdateConcurrencyException) when (intento < ReintentosConcurrencia)
            {
                // Otra reserva modificó los cupos al mismo tiempo: se recargan los datos y se vuelve a validar.
                db.LimpiarSeguimiento();
            }
        }
    }

    public async Task<Paginado<ReservaTuristaDto>> MisReservasAsync(string? filtro, int? pagina, CancellationToken ct = default)
    {
        var usuarioId = usuarioActual.IdRequerido;
        var hoy = reloj.Hoy;
        var consulta = db.Reservas.AsNoTracking().Where(r => r.UsuarioId == usuarioId);

        consulta = filtro switch
        {
            null or "" or "todas" => consulta,
            "proximas" => consulta.Where(r => (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada) &&
                                              r.Actividad.FechaFin >= hoy),
            "pasadas" => consulta.Where(r => (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada) &&
                                             r.Actividad.FechaFin < hoy),
            "canceladas" => consulta.Where(r => r.Estado == EstadoReserva.Cancelada || r.Estado == EstadoReserva.Rechazada),
            _ => throw ValidacionException.De("filtro", "Filtro no válido. Usa: proximas, pasadas o canceladas."),
        };

        var orden = filtro == "proximas"
            ? consulta.OrderBy(r => r.Actividad.FechaInicio).ThenBy(r => r.Id)
            : consulta.OrderByDescending(r => r.Actividad.FechaInicio).ThenByDescending(r => r.Id);

        var (p, t) = PaginadoExtensions.Normalizar(pagina, 10);
        var resultado = await orden.Include(r => r.Actividad).ThenInclude(a => a.Empresa).PaginarAsync(p, t, ct);

        var actividadIds = resultado.Items.Select(r => r.ActividadId).Distinct().ToList();
        var resenadas = (await db.Resenas.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && actividadIds.Contains(x.ActividadId))
            .Select(x => x.ActividadId).ToListAsync(ct)).ToHashSet();

        return new Paginado<ReservaTuristaDto>(
            resultado.Items.Select(r => ATurista(r, r.Actividad, resenadas.Contains(r.ActividadId))).ToList(),
            resultado.Pagina, resultado.Tamano, resultado.Total);
    }

    public async Task<ReservaTuristaDto> CancelarAsync(int id, string? motivo, CancellationToken ct = default)
    {
        var usuarioId = usuarioActual.IdRequerido;
        var reserva = await db.Reservas.Include(r => r.Actividad).ThenInclude(a => a.Empresa)
                          .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId, ct)
                      ?? throw new NoEncontradoException("La reserva no existe.");
        if (motivo?.Length > 300)
            throw ValidacionException.De("motivo", "El motivo no puede superar los 300 caracteres.");

        reserva.CancelarPorTurista(reloj.Hoy, DiasCancelacion, reloj.UtcAhora, motivo);
        await db.SaveChangesAsync(ct);
        return ATurista(reserva, reserva.Actividad, resenada: false);
    }

    public async Task CrearResenaAsync(int actividadId, CrearResenaRequest solicitud, CancellationToken ct = default)
    {
        await validadorResena.ValidarYLanzarAsync(solicitud, ct);
        var usuarioId = usuarioActual.IdRequerido;
        var hoy = reloj.Hoy;

        var completo = await db.Reservas.AnyAsync(r => r.UsuarioId == usuarioId && r.ActividadId == actividadId &&
                                                      r.Estado == EstadoReserva.Confirmada && r.Actividad.FechaFin < hoy, ct);
        if (!completo)
            throw new ReglaNegocioException("Solo puedes reseñar actividades que ya realizaste con una reserva confirmada.");
        if (await db.Resenas.AnyAsync(r => r.UsuarioId == usuarioId && r.ActividadId == actividadId, ct))
            throw new ConflictoException("Ya escribiste una reseña para esta actividad.");

        db.Resenas.Add(new Resena
        {
            UsuarioId = usuarioId,
            ActividadId = actividadId,
            Valoracion = solicitud.Valoracion,
            Comentario = solicitud.Comentario.Trim(),
        });
        await db.SaveChangesAsync(ct);
    }

    // ================= Empresa =================

    public async Task<Paginado<ReservaEmpresaDto>> RecibidasAsync(
        int? actividadId, EstadoReserva? estado, string? q, int? pagina, CancellationToken ct = default)
    {
        var usuarioId = usuarioActual.IdRequerido;
        var hoy = reloj.Hoy;
        var consulta = db.Reservas.AsNoTracking().Where(r => r.Actividad.Empresa.UsuarioId == usuarioId);
        if (actividadId is not null) consulta = consulta.Where(r => r.ActividadId == actividadId);
        if (estado is not null) consulta = consulta.Where(r => r.Estado == estado);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var texto = q.Trim().ToLowerInvariant();
            consulta = consulta.Where(r => r.Codigo.ToLower().Contains(texto) || r.Usuario.Nombre.ToLower().Contains(texto) ||
                                           r.Usuario.Apellidos.ToLower().Contains(texto) || r.Usuario.Correo.Contains(texto));
        }

        var (p, t) = PaginadoExtensions.Normalizar(pagina, 20);
        var resultado = await consulta
            .OrderBy(r => r.Estado != EstadoReserva.Pendiente).ThenBy(r => r.Actividad.FechaInicio).ThenByDescending(r => r.Id)
            .Select(r => new
            {
                r.Id, r.Codigo, r.ActividadId, Actividad = r.Actividad.Nombre, r.Actividad.FechaInicio, r.Actividad.FechaFin,
                r.Usuario.Nombre, r.Usuario.Apellidos, r.Usuario.Correo, r.Usuario.Telefono, r.Usuario.Dni,
                r.CantidadPersonas, r.Total, r.Estado, r.Notas, r.MotivoEstado, r.CreadoEn,
            })
            .PaginarAsync(p, t, ct);

        return new Paginado<ReservaEmpresaDto>(
            resultado.Items.Select(r => new ReservaEmpresaDto(
                r.Id, r.Codigo, r.ActividadId, r.Actividad, r.FechaInicio, $"{r.Nombre} {r.Apellidos}", r.Correo,
                r.Telefono, r.Dni, r.CantidadPersonas, r.Total,
                EstadoReservaExtensions.AVista(r.Estado, r.FechaFin, hoy), r.Notas, r.MotivoEstado, r.CreadoEn)).ToList(),
            resultado.Pagina, resultado.Tamano, resultado.Total);
    }

    public async Task ConfirmarAsync(int id, CancellationToken ct = default)
    {
        var reserva = await ReservaDeMiEmpresaAsync(id, ct);
        if (reserva.Actividad.HaFinalizado(reloj.Hoy))
            throw new ReglaNegocioException("La actividad ya finalizó.");
        reserva.Confirmar();
        await db.SaveChangesAsync(ct);
    }

    public async Task RechazarAsync(int id, string? motivo, CancellationToken ct = default)
    {
        var motivoLimpio = Validacion.Limpiar(motivo);
        if (motivoLimpio is null || motivoLimpio.Length is < 5 or > 300)
            throw ValidacionException.De("motivo", "Indica el motivo del rechazo (entre 5 y 300 caracteres).");

        var reserva = await ReservaDeMiEmpresaAsync(id, ct);
        reserva.Rechazar(motivoLimpio);
        await db.SaveChangesAsync(ct);
    }

    // ================= Auxiliares =================

    private async Task<Reserva> ReservaDeMiEmpresaAsync(int id, CancellationToken ct)
    {
        var usuarioId = usuarioActual.IdRequerido;
        return await db.Reservas.Include(r => r.Actividad)
                   .FirstOrDefaultAsync(r => r.Id == id && r.Actividad.Empresa.UsuarioId == usuarioId, ct)
               ?? throw new NoEncontradoException("La reserva no existe.");
    }

    private ReservaTuristaDto ATurista(Reserva r, Actividad a, bool resenada)
    {
        var hoy = reloj.Hoy;
        r.Actividad = a;
        var estado = EstadoReservaExtensions.AVista(r.Estado, a.FechaFin, hoy);
        return new ReservaTuristaDto(
            r.Id, r.Codigo,
            new ActividadReservaDto(a.Id, a.Nombre, a.Destino, a.FechaInicio, a.FechaFin, a.PuntoEncuentro, a.ImagenUrl,
                a.Empresa?.NombreComercial ?? "", a.Empresa?.Telefono ?? ""),
            r.CantidadPersonas, r.PrecioUnitario, r.Total, estado, r.MotivoEstado, r.Notas, r.CreadoEn,
            r.PuedeCancelarTurista(hoy, DiasCancelacion),
            a.FechaInicio.AddDays(-DiasCancelacion),
            estado == EstadoReservaVista.Completada && !resenada,
            resenada);
    }
}
