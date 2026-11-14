using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Application.Comun;
using TurismoGo.Domain;
using TurismoGo.Domain.Entidades;

namespace TurismoGo.Application.Cuentas;

public interface ICuentasService
{
    Task<SesionDto> LoginAsync(LoginRequest solicitud, CancellationToken ct = default);
    Task<SesionDto> RegistrarTuristaAsync(RegistroTuristaRequest solicitud, CancellationToken ct = default);
    Task<SesionDto> RegistrarEmpresaAsync(RegistroEmpresaRequest solicitud, CancellationToken ct = default);
    Task<UsuarioSesionDto> ObtenerSesionAsync(CancellationToken ct = default);
    Task<UsuarioSesionDto> ActualizarPerfilAsync(ActualizarPerfilRequest solicitud, CancellationToken ct = default);
    Task CambiarPasswordAsync(CambiarPasswordRequest solicitud, CancellationToken ct = default);
}

internal sealed class CuentasService(
    IAppDbContext db,
    IHasherContrasenas hasher,
    IGeneradorToken generadorToken,
    IUsuarioActual usuarioActual,
    IReloj reloj,
    IValidator<LoginRequest> validadorLogin,
    IValidator<RegistroTuristaRequest> validadorTurista,
    IValidator<RegistroEmpresaRequest> validadorEmpresa,
    IValidator<ActualizarPerfilRequest> validadorPerfil,
    IValidator<CambiarPasswordRequest> validadorPassword) : ICuentasService
{
    private const string MensajeCredenciales = "Correo o contraseña incorrectos.";

    // Hash real de un valor aleatorio: iguala el tiempo de respuesta cuando el correo no existe
    // (evita revelar qué correos están registrados).
    private static string? _hashFicticio;

    public async Task<SesionDto> LoginAsync(LoginRequest solicitud, CancellationToken ct = default)
    {
        await validadorLogin.ValidarYLanzarAsync(solicitud, ct);
        var correo = Validacion.NormalizarCorreo(solicitud.Correo);

        var usuario = await db.Usuarios.Include(u => u.Empresa).FirstOrDefaultAsync(u => u.Correo == correo, ct);
        if (usuario is null)
        {
            _hashFicticio ??= hasher.Hash(Guid.NewGuid().ToString());
            hasher.Verificar(_hashFicticio, solicitud.Password);
            throw new NoAutorizadoException(MensajeCredenciales);
        }
        if (!hasher.Verificar(usuario.PasswordHash, solicitud.Password))
            throw new NoAutorizadoException(MensajeCredenciales);
        if (!usuario.Activo)
            throw new ProhibidoException("Tu cuenta está suspendida. Comunícate con el equipo de TurismoGo.");

        usuario.UltimoAccesoEn = reloj.UtcAhora;
        await db.SaveChangesAsync(ct);
        return CrearSesion(usuario);
    }

    public async Task<SesionDto> RegistrarTuristaAsync(RegistroTuristaRequest solicitud, CancellationToken ct = default)
    {
        await validadorTurista.ValidarYLanzarAsync(solicitud, ct);
        var correo = Validacion.NormalizarCorreo(solicitud.Correo);
        var dni = solicitud.Dni.Trim();

        await AsegurarCorreoDisponibleAsync(correo, null, ct);
        await AsegurarDniDisponibleAsync(dni, null, ct);

        var usuario = new Usuario
        {
            Nombre = solicitud.Nombre.Trim(),
            Apellidos = solicitud.Apellidos.Trim(),
            Correo = correo,
            Dni = dni,
            Telefono = Validacion.Limpiar(solicitud.Telefono),
            PasswordHash = hasher.Hash(solicitud.Password),
            Rol = Rol.Turista,
            UltimoAccesoEn = reloj.UtcAhora,
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);
        return CrearSesion(usuario);
    }

    public async Task<SesionDto> RegistrarEmpresaAsync(RegistroEmpresaRequest solicitud, CancellationToken ct = default)
    {
        await validadorEmpresa.ValidarYLanzarAsync(solicitud, ct);
        var correo = Validacion.NormalizarCorreo(solicitud.Correo);
        var ruc = solicitud.Ruc.Trim();

        await AsegurarCorreoDisponibleAsync(correo, null, ct);
        if (await db.Empresas.AnyAsync(e => e.Ruc == ruc, ct))
            throw new ConflictoException("Ya existe una empresa registrada con ese RUC.");

        var usuario = new Usuario
        {
            Nombre = solicitud.Nombre.Trim(),
            Apellidos = solicitud.Apellidos.Trim(),
            Correo = correo,
            Telefono = solicitud.Telefono.Trim(),
            PasswordHash = hasher.Hash(solicitud.Password),
            Rol = Rol.Empresa,
            UltimoAccesoEn = reloj.UtcAhora,
            Empresa = new Empresa
            {
                RazonSocial = solicitud.RazonSocial.Trim(),
                NombreComercial = solicitud.NombreComercial.Trim(),
                Ruc = ruc,
                Telefono = solicitud.Telefono.Trim(),
                Direccion = solicitud.Direccion.Trim(),
                Estado = EstadoEmpresa.Pendiente,
            },
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);
        return CrearSesion(usuario);
    }

    public async Task<UsuarioSesionDto> ObtenerSesionAsync(CancellationToken ct = default) =>
        AUsuarioSesion(await ObtenerUsuarioActualAsync(ct));

    public async Task<UsuarioSesionDto> ActualizarPerfilAsync(ActualizarPerfilRequest solicitud, CancellationToken ct = default)
    {
        await validadorPerfil.ValidarYLanzarAsync(solicitud, ct);
        var usuario = await ObtenerUsuarioActualAsync(ct);

        var dni = Validacion.Limpiar(solicitud.Dni);
        if (usuario.Rol == Rol.Turista && dni is null)
            throw ValidacionException.De("dni", "El DNI es obligatorio para los turistas.");
        if (dni is not null)
            await AsegurarDniDisponibleAsync(dni, usuario.Id, ct);

        usuario.Nombre = solicitud.Nombre.Trim();
        usuario.Apellidos = solicitud.Apellidos.Trim();
        usuario.Telefono = Validacion.Limpiar(solicitud.Telefono);
        usuario.Dni = dni;
        await db.SaveChangesAsync(ct);
        return AUsuarioSesion(usuario);
    }

    public async Task CambiarPasswordAsync(CambiarPasswordRequest solicitud, CancellationToken ct = default)
    {
        await validadorPassword.ValidarYLanzarAsync(solicitud, ct);
        var usuario = await ObtenerUsuarioActualAsync(ct);
        if (!hasher.Verificar(usuario.PasswordHash, solicitud.PasswordActual))
            throw ValidacionException.De("passwordActual", "La contraseña actual no es correcta.");

        usuario.PasswordHash = hasher.Hash(solicitud.PasswordNueva);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Usuario> ObtenerUsuarioActualAsync(CancellationToken ct)
    {
        var id = usuarioActual.IdRequerido;
        return await db.Usuarios.Include(u => u.Empresa).FirstOrDefaultAsync(u => u.Id == id, ct)
               ?? throw new NoAutorizadoException("Tu sesión ya no es válida. Inicia sesión nuevamente.");
    }

    private async Task AsegurarCorreoDisponibleAsync(string correo, int? excluirId, CancellationToken ct)
    {
        if (await db.Usuarios.AnyAsync(u => u.Correo == correo && u.Id != excluirId, ct))
            throw new ConflictoException("Ya existe una cuenta con ese correo electrónico.");
    }

    private async Task AsegurarDniDisponibleAsync(string dni, int? excluirId, CancellationToken ct)
    {
        if (await db.Usuarios.AnyAsync(u => u.Dni == dni && u.Id != excluirId, ct))
            throw new ConflictoException("Ya existe una cuenta con ese DNI.");
    }

    private SesionDto CrearSesion(Usuario usuario)
    {
        var token = generadorToken.Generar(usuario);
        return new SesionDto(token.Token, token.ExpiraEn, AUsuarioSesion(usuario));
    }

    internal static UsuarioSesionDto AUsuarioSesion(Usuario u) => new(
        u.Id, u.Nombre, u.Apellidos, u.Correo, u.Dni, u.Telefono, u.Rol,
        u.Empresa is null ? null : new EmpresaSesionDto(u.Empresa.Id, u.Empresa.NombreComercial, u.Empresa.Estado, u.Empresa.MotivoEstado));
}
