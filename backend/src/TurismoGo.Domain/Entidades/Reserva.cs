namespace TurismoGo.Domain.Entidades;

/// <summary>Reserva de una actividad hecha por un turista.</summary>
public class Reserva : EntidadAuditable
{
    public int Id { get; set; }

    /// <summary>Código legible para el cliente (por ejemplo, TG-7KQ2M9XA). Único.</summary>
    public string Codigo { get; set; } = string.Empty;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public int ActividadId { get; set; }
    public Actividad Actividad { get; set; } = null!;

    public int CantidadPersonas { get; set; }

    /// <summary>Precio por persona al momento de reservar (los cambios posteriores no afectan).</summary>
    public decimal PrecioUnitario { get; set; }

    public decimal Total { get; set; }
    public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;
    public string? Notas { get; set; }

    /// <summary>Motivo del rechazo o de la cancelación.</summary>
    public string? MotivoEstado { get; set; }

    public DateTime? CanceladaEn { get; set; }

    public bool EstaActiva => Estado is EstadoReserva.Pendiente or EstadoReserva.Confirmada;

    public bool EstaCompletada(DateOnly hoy) =>
        Estado == EstadoReserva.Confirmada && Actividad is not null && Actividad.HaFinalizado(hoy);

    /// <summary>El turista puede cancelar si está activa y faltan al menos <paramref name="diasMinimos"/> días.</summary>
    public bool PuedeCancelarTurista(DateOnly hoy, int diasMinimos) =>
        EstaActiva && Actividad is not null && Actividad.FechaInicio.DayNumber - hoy.DayNumber >= diasMinimos;

    public void CancelarPorTurista(DateOnly hoy, int diasMinimos, DateTime ahoraUtc, string? motivo)
    {
        if (!EstaActiva)
            throw new ReglaNegocioException("Esta reserva ya no está activa.");
        if (!PuedeCancelarTurista(hoy, diasMinimos))
            throw new ReglaNegocioException(
                $"Solo puedes cancelar hasta {diasMinimos} días antes del inicio de la actividad.");
        Cancelar(ahoraUtc, string.IsNullOrWhiteSpace(motivo) ? "Cancelada por el cliente." : motivo.Trim());
    }

    public void Cancelar(DateTime ahoraUtc, string motivo)
    {
        Estado = EstadoReserva.Cancelada;
        MotivoEstado = motivo;
        CanceladaEn = ahoraUtc;
        Actividad.LiberarCupos(CantidadPersonas);
    }

    public void Confirmar()
    {
        if (Estado != EstadoReserva.Pendiente)
            throw new ReglaNegocioException("Solo se pueden confirmar reservas pendientes.");
        Estado = EstadoReserva.Confirmada;
        MotivoEstado = null;
    }

    public void Rechazar(string motivo)
    {
        if (Estado != EstadoReserva.Pendiente)
            throw new ReglaNegocioException("Solo se pueden rechazar reservas pendientes.");
        Estado = EstadoReserva.Rechazada;
        MotivoEstado = motivo;
        Actividad.LiberarCupos(CantidadPersonas);
    }
}
