namespace TurismoGo.Domain.Entidades;

/// <summary>Actividad o excursión ofrecida por una empresa de turismo.</summary>
public class Actividad : EntidadAuditable
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;

    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string PuntoEncuentro { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }

    /// <summary>Precio por persona en soles (PEN).</summary>
    public decimal Precio { get; set; }

    /// <summary>Número máximo de personas.</summary>
    public int Capacidad { get; set; }

    /// <summary>Personas en reservas activas (pendientes o confirmadas).</summary>
    public int CuposReservados { get; set; }

    public string? Incluye { get; set; }
    public string? NoIncluye { get; set; }
    public string? ImagenUrl { get; set; }
    public EstadoActividad Estado { get; set; } = EstadoActividad.Borrador;

    /// <summary>Token de concurrencia optimista: evita la sobreventa de cupos.</summary>
    public Guid Version { get; set; } = Guid.NewGuid();

    public List<ItinerarioDia> Itinerario { get; set; } = [];
    public ICollection<Reserva> Reservas { get; set; } = [];
    public ICollection<Resena> Resenas { get; set; } = [];

    public int CuposDisponibles => Math.Max(0, Capacidad - CuposReservados);
    public int DuracionDias => FechaFin.DayNumber - FechaInicio.DayNumber + 1;

    public bool HaFinalizado(DateOnly hoy) => FechaFin < hoy;

    /// <summary>Se puede reservar si está publicada, no ha empezado y quedan cupos.</summary>
    public bool EsReservable(DateOnly hoy) =>
        Estado == EstadoActividad.Publicada && FechaInicio > hoy && CuposDisponibles > 0;

    public void ReservarCupos(int personas, DateOnly hoy)
    {
        if (Estado != EstadoActividad.Publicada)
            throw new ReglaNegocioException("La actividad no está disponible para reservas.");
        if (FechaInicio <= hoy)
            throw new ReglaNegocioException("La actividad ya comenzó o empieza hoy; no admite nuevas reservas.");
        if (personas <= 0)
            throw new ReglaNegocioException("La cantidad de personas debe ser mayor que cero.");
        if (personas > CuposDisponibles)
            throw new ReglaNegocioException(CuposDisponibles == 0
                ? "No quedan cupos disponibles para esta actividad."
                : $"Solo quedan {CuposDisponibles} cupos disponibles.");

        CuposReservados += personas;
        Version = Guid.NewGuid();
    }

    public void LiberarCupos(int personas)
    {
        CuposReservados = Math.Max(0, CuposReservados - personas);
        Version = Guid.NewGuid();
    }

    public void Publicar(DateOnly hoy)
    {
        if (Estado == EstadoActividad.Cancelada)
            throw new ReglaNegocioException("Una actividad cancelada no se puede publicar.");
        if (Estado == EstadoActividad.Publicada)
            throw new ReglaNegocioException("La actividad ya está publicada.");
        if (FechaInicio <= hoy)
            throw new ReglaNegocioException("La fecha de inicio debe ser posterior a hoy para publicar.");
        if (Itinerario.Count == 0)
            throw new ReglaNegocioException("Agrega al menos un día de itinerario antes de publicar.");
        Estado = EstadoActividad.Publicada;
    }
}

/// <summary>Un día (o tramo) del itinerario de una actividad.</summary>
public class ItinerarioDia
{
    public int Id { get; set; }
    public int ActividadId { get; set; }
    public int Dia { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}
