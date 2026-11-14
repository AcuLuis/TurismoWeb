using Microsoft.EntityFrameworkCore;
using TurismoGo.Domain;
using TurismoGo.Domain.Entidades;

namespace TurismoGo.Application.Abstracciones;

/// <summary>Acceso a datos que usa la capa de aplicación (implementado por EF Core en Infrastructure).</summary>
public interface IAppDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Empresa> Empresas { get; }
    DbSet<Actividad> Actividades { get; }
    DbSet<ItinerarioDia> ItinerarioDias { get; }
    DbSet<Reserva> Reservas { get; }
    DbSet<Resena> Resenas { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Descarta las entidades en seguimiento (útil para reintentar tras un conflicto de concurrencia).</summary>
    void LimpiarSeguimiento();
}

/// <summary>Fecha y hora del sistema. Permite pruebas deterministas.</summary>
public interface IReloj
{
    DateTime UtcAhora { get; }

    /// <summary>Fecha actual en la zona horaria del negocio (America/Lima).</summary>
    DateOnly Hoy { get; }
}

public interface IHasherContrasenas
{
    string Hash(string contrasena);
    bool Verificar(string hash, string contrasena);
}

public sealed record TokenGenerado(string Token, DateTime ExpiraEn);

public interface IGeneradorToken
{
    TokenGenerado Generar(Usuario usuario);
}

/// <summary>Usuario autenticado de la petición actual.</summary>
public interface IUsuarioActual
{
    int? Id { get; }
    Rol? Rol { get; }

    /// <summary>Id del usuario autenticado o excepción 401 si no hay sesión.</summary>
    int IdRequerido { get; }
}
