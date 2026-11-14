using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TurismoGo.Infrastructure.Datos;

/// <summary>
/// Usada solo por las herramientas de EF (dotnet ef migrations ...). No se conecta a la BD para generar migraciones.
/// </summary>
internal sealed class DisenoDbContextFactory : IDesignTimeDbContextFactory<TurismoDbContext>
{
    public TurismoDbContext CreateDbContext(string[] args)
    {
        var cadena = Environment.GetEnvironmentVariable("ConnectionStrings__TurismoGo")
                     ?? "Server=127.0.0.1,14333;Database=TurismoGo;User Id=sa;TrustServerCertificate=True";
        var opciones = new DbContextOptionsBuilder<TurismoDbContext>().UseSqlServer(cadena).Options;
        return new TurismoDbContext(opciones);
    }
}
