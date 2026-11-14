using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Domain;
using TurismoGo.Domain.Entidades;

namespace TurismoGo.Infrastructure.Datos;

/// <summary>Opciones de la sección "Inicializacion" de appsettings.</summary>
public sealed class InicializacionOptions
{
    public const string Seccion = "Inicializacion";

    /// <summary>Aplica las migraciones pendientes al iniciar (recomendado solo en desarrollo).</summary>
    public bool AplicarMigraciones { get; set; }

    /// <summary>Carga datos de demostración si la base está vacía (solo desarrollo).</summary>
    public bool SembrarDatosDemo { get; set; }

    /// <summary>Crea la cuenta de administrador indicada si todavía no existe ningún administrador.</summary>
    public string? AdminCorreo { get; set; }
    public string? AdminPassword { get; set; }
}

public sealed partial class InicializadorDatos(
    TurismoDbContext db,
    IHasherContrasenas hasher,
    IReloj reloj,
    IOptions<InicializacionOptions> opciones,
    ILogger<InicializadorDatos> logger)
{
    private readonly InicializacionOptions _opciones = opciones.Value;

    public async Task EjecutarAsync(CancellationToken ct = default)
    {
        if (_opciones.AplicarMigraciones)
        {
            LogMigrando(logger);
            await db.Database.MigrateAsync(ct);
        }

        await CrearAdministradorAsync(ct);

        if (_opciones.SembrarDatosDemo && !await db.Actividades.AnyAsync(ct))
        {
            LogSembrando(logger);
            await new DatosDemo(db, hasher, reloj).SembrarAsync(ct);
        }
    }

    private async Task CrearAdministradorAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opciones.AdminCorreo) || string.IsNullOrWhiteSpace(_opciones.AdminPassword))
            return;
        if (await db.Usuarios.AnyAsync(u => u.Rol == Rol.Admin, ct))
            return;

        db.Usuarios.Add(new Usuario
        {
            Nombre = "Administrador",
            Apellidos = "TurismoGo",
            Correo = _opciones.AdminCorreo.Trim().ToLowerInvariant(),
            PasswordHash = hasher.Hash(_opciones.AdminPassword),
            Rol = Rol.Admin,
        });
        await db.SaveChangesAsync(ct);
        LogAdminCreado(logger, _opciones.AdminCorreo);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Aplicando migraciones pendientes...")]
    private static partial void LogMigrando(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cargando datos de demostración...")]
    private static partial void LogSembrando(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cuenta de administrador inicial creada: {Correo}")]
    private static partial void LogAdminCreado(ILogger logger, string correo);
}
