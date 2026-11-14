namespace TurismoGo.Domain.Entidades;

/// <summary>Campos de auditoría que se completan automáticamente al guardar.</summary>
public abstract class EntidadAuditable
{
    public DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
}
