using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurismoGo.Domain.Entidades;

namespace TurismoGo.Infrastructure.Datos;

internal sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("Usuarios");
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.Property(x => x.Apellidos).HasMaxLength(100).IsRequired();
        b.Property(x => x.Correo).HasMaxLength(100).IsRequired();
        b.Property(x => x.Dni).HasMaxLength(8).IsFixedLength(false);
        b.Property(x => x.Telefono).HasMaxLength(20);
        b.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
        b.Property(x => x.Rol).HasConversion<string>().HasMaxLength(20);
        b.Ignore(x => x.NombreCompleto);

        b.HasIndex(x => x.Correo).IsUnique();
        b.HasIndex(x => x.Dni).IsUnique().HasFilter("[Dni] IS NOT NULL");
    }
}

internal sealed class EmpresaConfiguracion : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> b)
    {
        b.ToTable("Empresas");
        b.Property(x => x.RazonSocial).HasMaxLength(150).IsRequired();
        b.Property(x => x.NombreComercial).HasMaxLength(100).IsRequired();
        b.Property(x => x.Ruc).HasMaxLength(11).IsRequired();
        b.Property(x => x.Telefono).HasMaxLength(20).IsRequired();
        b.Property(x => x.Direccion).HasMaxLength(200).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(1000);
        b.Property(x => x.LogoUrl).HasMaxLength(500);
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.MotivoEstado).HasMaxLength(300);
        b.Ignore(x => x.PuedePublicar);

        b.HasIndex(x => x.Ruc).IsUnique();
        b.HasIndex(x => x.UsuarioId).IsUnique();
        b.HasOne(x => x.Usuario).WithOne(u => u.Empresa)
            .HasForeignKey<Empresa>(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ActividadConfiguracion : IEntityTypeConfiguration<Actividad>
{
    public void Configure(EntityTypeBuilder<Actividad> b)
    {
        b.ToTable("Actividades", t =>
        {
            t.HasCheckConstraint("CK_Actividades_Fechas", "[FechaFin] >= [FechaInicio]");
            t.HasCheckConstraint("CK_Actividades_Precio", "[Precio] > 0");
            t.HasCheckConstraint("CK_Actividades_Capacidad", "[Capacidad] > 0");
            t.HasCheckConstraint("CK_Actividades_Cupos", "[CuposReservados] >= 0 AND [CuposReservados] <= [Capacidad]");
        });
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Destino).HasMaxLength(80).IsRequired();
        b.Property(x => x.PuntoEncuentro).HasMaxLength(200).IsRequired();
        b.Property(x => x.Precio).HasPrecision(10, 2);
        b.Property(x => x.Incluye).HasMaxLength(1000);
        b.Property(x => x.NoIncluye).HasMaxLength(1000);
        b.Property(x => x.ImagenUrl).HasMaxLength(500);
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.Ignore(x => x.CuposDisponibles);
        b.Ignore(x => x.DuracionDias);

        b.HasIndex(x => new { x.Estado, x.FechaInicio });
        b.HasIndex(x => x.Destino);
        b.HasOne(x => x.Empresa).WithMany(e => e.Actividades)
            .HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Itinerario).WithOne()
            .HasForeignKey(i => i.ActividadId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ItinerarioDiaConfiguracion : IEntityTypeConfiguration<ItinerarioDia>
{
    public void Configure(EntityTypeBuilder<ItinerarioDia> b)
    {
        b.ToTable("ItinerarioDias");
        b.Property(x => x.Titulo).HasMaxLength(100).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(1000).IsRequired();
        b.HasIndex(x => new { x.ActividadId, x.Dia });
    }
}

internal sealed class ReservaConfiguracion : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> b)
    {
        b.ToTable("Reservas", t =>
        {
            t.HasCheckConstraint("CK_Reservas_Personas", "[CantidadPersonas] > 0");
            t.HasCheckConstraint("CK_Reservas_Total", "[Total] >= 0");
        });
        b.Property(x => x.Codigo).HasMaxLength(12).IsRequired();
        b.Property(x => x.PrecioUnitario).HasPrecision(10, 2);
        b.Property(x => x.Total).HasPrecision(12, 2);
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Notas).HasMaxLength(500);
        b.Property(x => x.MotivoEstado).HasMaxLength(300);
        b.Ignore(x => x.EstaActiva);

        b.HasIndex(x => x.Codigo).IsUnique();
        b.HasIndex(x => new { x.UsuarioId, x.Estado });
        b.HasIndex(x => new { x.ActividadId, x.Estado });
        b.HasOne(x => x.Usuario).WithMany(u => u.Reservas)
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Actividad).WithMany(a => a.Reservas)
            .HasForeignKey(x => x.ActividadId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ResenaConfiguracion : IEntityTypeConfiguration<Resena>
{
    public void Configure(EntityTypeBuilder<Resena> b)
    {
        b.ToTable("Resenas", t => t.HasCheckConstraint("CK_Resenas_Valoracion", "[Valoracion] BETWEEN 1 AND 5"));
        b.Property(x => x.Comentario).HasMaxLength(1000).IsRequired();
        b.Property(x => x.MotivoOcultamiento).HasMaxLength(300);

        b.HasIndex(x => new { x.UsuarioId, x.ActividadId }).IsUnique();
        b.HasIndex(x => new { x.ActividadId, x.Oculta });
        b.HasOne(x => x.Usuario).WithMany(u => u.Resenas)
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Actividad).WithMany(a => a.Resenas)
            .HasForeignKey(x => x.ActividadId).OnDelete(DeleteBehavior.Restrict);
    }
}
