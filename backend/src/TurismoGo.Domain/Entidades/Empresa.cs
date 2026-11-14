namespace TurismoGo.Domain.Entidades;

/// <summary>Perfil de empresa de turismo. Pertenece a un usuario con rol Empresa (relación 1:1).</summary>
public class Empresa : EntidadAuditable
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string RazonSocial { get; set; } = string.Empty;
    public string NombreComercial { get; set; } = string.Empty;

    /// <summary>RUC de 11 dígitos. Único.</summary>
    public string Ruc { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? LogoUrl { get; set; }

    public EstadoEmpresa Estado { get; set; } = EstadoEmpresa.Pendiente;

    /// <summary>Motivo de la última suspensión (visible para la empresa).</summary>
    public string? MotivoEstado { get; set; }

    public DateTime? VerificadaEn { get; set; }

    public ICollection<Actividad> Actividades { get; set; } = [];

    public bool PuedePublicar => Estado == EstadoEmpresa.Verificada;

    public void Verificar(DateTime ahoraUtc)
    {
        Estado = EstadoEmpresa.Verificada;
        MotivoEstado = null;
        VerificadaEn = ahoraUtc;
    }

    public void Suspender(string motivo)
    {
        Estado = EstadoEmpresa.Suspendida;
        MotivoEstado = motivo;
    }
}
