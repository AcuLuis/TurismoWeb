using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TurismoGo.Application.Actividades;
using TurismoGo.Application.Cuentas;
using TurismoGo.Application.Empresas;
using TurismoGo.Application.Reservas;

namespace TurismoGo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Validadores: todos los AbstractValidator<T> del ensamblado (sin estado → singleton)
        var tipos = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true });
        foreach (var tipo in tipos)
        {
            var interfaz = tipo.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));
            if (interfaz is not null)
                services.AddSingleton(interfaz, tipo);
        }

        services.AddScoped<ICuentasService, CuentasService>();
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<IActividadesEmpresaService, ActividadesEmpresaService>();
        services.AddScoped<IReservasService, ReservasService>();
        services.AddScoped<IEmpresaPerfilService, EmpresaPerfilService>();
        return services;
    }
}
