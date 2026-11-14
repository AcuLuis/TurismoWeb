namespace TurismoGo.Domain.Entidades;

/// <summary>Cuenta de acceso al sistema (turista, empresa o administrador).</summary>
public class Usuario : EntidadAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;

    /// <summary>Correo normalizado en minúsculas. Único.</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>DNI (8 dígitos). Obligatorio para turistas. Único cuando existe.</summary>
    public string? Dni { get; set; }

    public string? Telefono { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public Rol Rol { get; set; }

    /// <summary>Borrado lógico / suspensión de la cuenta.</summary>
    public bool Activo { get; set; } = true;

    public DateTime? UltimoAccesoEn { get; set; }

    public Empresa? Empresa { get; set; }
    public ICollection<Reserva> Reservas { get; set; } = [];
    public ICollection<Resena> Resenas { get; set; } = [];

    public string NombreCompleto => $"{Nombre} {Apellidos}".Trim();
}
