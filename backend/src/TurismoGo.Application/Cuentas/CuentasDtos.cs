using TurismoGo.Domain;

namespace TurismoGo.Application.Cuentas;

public sealed record LoginRequest(string Correo, string Password);

public sealed record RegistroTuristaRequest(
    string Nombre, string Apellidos, string Dni, string Correo, string? Telefono, string Password);

public sealed record RegistroEmpresaRequest(
    string Nombre, string Apellidos, string Correo, string Telefono, string Password,
    string RazonSocial, string NombreComercial, string Ruc, string Direccion);

public sealed record EmpresaSesionDto(int Id, string NombreComercial, EstadoEmpresa Estado, string? MotivoEstado);

public sealed record UsuarioSesionDto(
    int Id, string Nombre, string Apellidos, string Correo, string? Dni, string? Telefono,
    Rol Rol, EmpresaSesionDto? Empresa);

public sealed record SesionDto(string Token, DateTime ExpiraEn, UsuarioSesionDto Usuario);

public sealed record ActualizarPerfilRequest(string Nombre, string Apellidos, string? Telefono, string? Dni);

public sealed record CambiarPasswordRequest(string PasswordActual, string PasswordNueva);
