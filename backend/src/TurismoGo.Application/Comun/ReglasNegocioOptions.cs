namespace TurismoGo.Application.Comun;

/// <summary>Parámetros configurables del negocio (sección "ReglasNegocio" de appsettings).</summary>
public sealed class ReglasNegocioOptions
{
    public const string Seccion = "ReglasNegocio";

    /// <summary>Días mínimos de anticipación para que el turista cancele una reserva.</summary>
    public int DiasMinimosCancelacion { get; set; } = 2;

    /// <summary>Máximo de personas por reserva.</summary>
    public int MaxPersonasPorReserva { get; set; } = 20;
}
