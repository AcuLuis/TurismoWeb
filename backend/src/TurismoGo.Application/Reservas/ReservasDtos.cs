using TurismoGo.Domain;

namespace TurismoGo.Application.Reservas;

/// <summary>Estado de reserva tal como se muestra al usuario (incluye "Completada", que es derivado).</summary>
public enum EstadoReservaVista
{
    Pendiente,
    Confirmada,
    Completada,
    Rechazada,
    Cancelada,
}

public sealed record CrearReservaRequest(int ActividadId, int CantidadPersonas, string? Notas);

public sealed record CrearResenaRequest(int Valoracion, string Comentario);

public sealed record ActividadReservaDto(
    int Id, string Nombre, string Destino, DateOnly FechaInicio, DateOnly FechaFin, string PuntoEncuentro,
    string? ImagenUrl, string Empresa, string EmpresaTelefono);

public sealed record ReservaTuristaDto(
    int Id, string Codigo, ActividadReservaDto Actividad, int CantidadPersonas, decimal PrecioUnitario, decimal Total,
    EstadoReservaVista Estado, string? MotivoEstado, string? Notas, DateTime CreadoEn,
    bool PuedeCancelar, DateOnly FechaLimiteCancelacion, bool PuedeResenar, bool Resenada);

public sealed record ReservaEmpresaDto(
    int Id, string Codigo, int ActividadId, string Actividad, DateOnly FechaInicio,
    string Turista, string Correo, string? Telefono, string? Dni,
    int CantidadPersonas, decimal Total, EstadoReservaVista Estado, string? Notas, string? MotivoEstado, DateTime CreadoEn);

public static class EstadoReservaExtensions
{
    public static EstadoReservaVista AVista(EstadoReserva estado, DateOnly fechaFin, DateOnly hoy) => estado switch
    {
        EstadoReserva.Confirmada when fechaFin < hoy => EstadoReservaVista.Completada,
        EstadoReserva.Confirmada => EstadoReservaVista.Confirmada,
        EstadoReserva.Pendiente => EstadoReservaVista.Pendiente,
        EstadoReserva.Rechazada => EstadoReservaVista.Rechazada,
        _ => EstadoReservaVista.Cancelada,
    };
}
