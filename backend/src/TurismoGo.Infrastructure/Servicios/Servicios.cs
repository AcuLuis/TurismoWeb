using Microsoft.AspNetCore.Identity;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Domain.Entidades;

namespace TurismoGo.Infrastructure;

/// <summary>Reloj real. La fecha "de negocio" se calcula en la hora de Perú (UTC-5, sin horario de verano).</summary>
public sealed class RelojSistema : IReloj
{
    private static readonly TimeZoneInfo ZonaLima = ObtenerZonaLima();

    public DateTime UtcAhora => DateTime.UtcNow;
    public DateOnly Hoy => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaLima));

    private static TimeZoneInfo ObtenerZonaLima()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Lima"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("Lima", TimeSpan.FromHours(-5), "Lima", "Lima"); }
    }
}

/// <summary>Hash de contraseñas con PBKDF2 (implementación de ASP.NET Core Identity).</summary>
public sealed class HasherContrasenas : IHasherContrasenas
{
    private readonly PasswordHasher<Usuario> _hasher = new();
    private static readonly Usuario Sujeto = new();

    public string Hash(string contrasena) => _hasher.HashPassword(Sujeto, contrasena);

    public bool Verificar(string hash, string contrasena)
    {
        try
        {
            return _hasher.VerifyHashedPassword(Sujeto, hash, contrasena) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
