using Microsoft.EntityFrameworkCore;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Domain.Entidades;

namespace TurismoGo.Infrastructure.Datos;

public sealed class TurismoDbContext(DbContextOptions<TurismoDbContext> options, IReloj? reloj = null)
    : DbContext(options), IAppDbContext
{
    private readonly IReloj _reloj = reloj ?? new RelojSistema();

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Actividad> Actividades => Set<Actividad>();
    public DbSet<ItinerarioDia> ItinerarioDias => Set<ItinerarioDia>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Resena> Resenas => Set<Resena>();

    public void LimpiarSeguimiento() => ChangeTracker.Clear();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TurismoDbContext).Assembly);

        // SQLite (solo pruebas) no ordena ni agrega columnas decimal: se almacenan como REAL.
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            foreach (var propiedad in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(t => t.GetProperties())
                         .Where(p => p.ClrType == typeof(decimal)))
            {
                propiedad.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.CastingConverter<decimal, double>());
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var ahora = _reloj.UtcAhora;
        foreach (var entrada in ChangeTracker.Entries<EntidadAuditable>())
        {
            if (entrada.State == EntityState.Added)
                entrada.Entity.CreadoEn = ahora;
            else if (entrada.State == EntityState.Modified)
                entrada.Entity.ActualizadoEn = ahora;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
