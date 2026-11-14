using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Infrastructure.Datos;

namespace TurismoGo.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra la persistencia (SQL Server) y los servicios de infraestructura.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuracion)
    {
        var cadena = configuracion.GetConnectionString("TurismoGo");
        if (!string.IsNullOrWhiteSpace(cadena))
        {
            services.AddDbContext<TurismoDbContext>(o =>
                o.UseSqlServer(cadena, sql => sql.EnableRetryOnFailure(3)));
        }

        return services.AddInfrastructureServicios(configuracion);
    }

    /// <summary>Servicios que no dependen del proveedor de base de datos (las pruebas registran su propio DbContext).</summary>
    public static IServiceCollection AddInfrastructureServicios(this IServiceCollection services, IConfiguration configuracion)
    {
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<TurismoDbContext>());
        services.AddSingleton<IReloj, RelojSistema>();
        services.AddSingleton<IHasherContrasenas, HasherContrasenas>();
        services.Configure<InicializacionOptions>(configuracion.GetSection(InicializacionOptions.Seccion));
        services.AddScoped<InicializadorDatos>();
        services.AddHealthChecks().AddDbContextCheck<TurismoDbContext>("base-de-datos");
        return services;
    }
}
