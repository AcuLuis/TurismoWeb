using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Application.Comun;
using TurismoGo.Domain;
using TurismoGo.Domain.Entidades;
using TurismoGo.Infrastructure;
using TurismoGo.Infrastructure.Datos;

namespace TurismoGo.Tests.Infra;

/// <summary>Reloj controlable para pruebas deterministas.</summary>
public sealed class RelojFijo(DateOnly hoy) : IReloj
{
    public DateOnly Hoy { get; set; } = hoy;
    public DateTime UtcAhora => Hoy.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
}

public sealed class UsuarioActualFijo(int? id, Rol? rol) : IUsuarioActual
{
    public int? Id { get; set; } = id;
    public Rol? Rol { get; set; } = rol;
    public int IdRequerido => Id ?? throw new NoAutorizadoException("Sin sesión.");
}

/// <summary>Base de datos SQLite en memoria (relacional real: respeta índices únicos y restricciones).</summary>
public sealed class BaseDatosPrueba : IDisposable
{
    private readonly SqliteConnection _conexion = new("DataSource=:memory:");

    public RelojFijo Reloj { get; } = new(new DateOnly(2026, 11, 14));
    public TurismoDbContext Db { get; }

    public BaseDatosPrueba()
    {
        _conexion.Open();
        Db = NuevoContexto();
        Db.Database.EnsureCreated();
    }

    /// <summary>Contexto adicional sobre la misma base (simula otra petición concurrente).</summary>
    public TurismoDbContext NuevoContexto() =>
        new(new DbContextOptionsBuilder<TurismoDbContext>().UseSqlite(_conexion).Options, Reloj);

    public Empresa CrearEmpresa(EstadoEmpresa estado = EstadoEmpresa.Verificada, string ruc = "20100000001")
    {
        var usuario = new Usuario
        {
            Nombre = "Rosa", Apellidos = "Huamán", Correo = $"empresa{ruc}@test.pe", PasswordHash = "x", Rol = Rol.Empresa,
            Empresa = new Empresa
            {
                RazonSocial = "Andes SAC", NombreComercial = "Andes", Ruc = ruc, Telefono = "984000000",
                Direccion = "Cusco", Estado = estado,
            },
        };
        Db.Usuarios.Add(usuario);
        Db.SaveChanges();
        return usuario.Empresa!;
    }

    public Usuario CrearTurista(string dni = "40000001")
    {
        var u = new Usuario
        {
            Nombre = "Ana", Apellidos = "Quispe", Correo = $"turista{dni}@test.pe", Dni = dni, PasswordHash = "x", Rol = Rol.Turista,
        };
        Db.Usuarios.Add(u);
        Db.SaveChanges();
        return u;
    }

    public Actividad CrearActividad(Empresa empresa, int diasHastaInicio = 10, int capacidad = 10, decimal precio = 100m,
        EstadoActividad estado = EstadoActividad.Publicada)
    {
        var inicio = Reloj.Hoy.AddDays(diasHastaInicio);
        var a = new Actividad
        {
            EmpresaId = empresa.Id, Nombre = "Tour de prueba", Descripcion = "Descripción suficientemente larga.",
            Destino = "Cusco", PuntoEncuentro = "Plaza", FechaInicio = inicio, FechaFin = inicio, Precio = precio,
            Capacidad = capacidad, Estado = estado,
            Itinerario = [new ItinerarioDia { Dia = 1, Titulo = "Día 1", Descripcion = "Recorrido" }],
        };
        Db.Actividades.Add(a);
        Db.SaveChanges();
        return a;
    }

    public void Dispose()
    {
        Db.Dispose();
        _conexion.Dispose();
    }
}

public static class Hasher
{
    public static readonly HasherContrasenas Instancia = new();
}
