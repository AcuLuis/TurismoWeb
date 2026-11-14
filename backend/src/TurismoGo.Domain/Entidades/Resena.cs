namespace TurismoGo.Domain.Entidades;

/// <summary>Reseña de un turista sobre una actividad que completó. Una por usuario y actividad.</summary>
public class Resena : EntidadAuditable
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public int ActividadId { get; set; }
    public Actividad Actividad { get; set; } = null!;

    /// <summary>De 1 a 5 estrellas.</summary>
    public int Valoracion { get; set; }

    public string Comentario { get; set; } = string.Empty;

    /// <summary>Moderación: una reseña oculta no se muestra ni cuenta para el promedio.</summary>
    public bool Oculta { get; set; }

    public string? MotivoOcultamiento { get; set; }
}
