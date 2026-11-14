using FluentValidation;
using TurismoGo.Application.Comun;

namespace TurismoGo.Application.Cuentas;

public sealed class LoginValidador : AbstractValidator<LoginRequest>
{
    public LoginValidador()
    {
        RuleFor(x => x.Correo).NotEmpty().WithMessage("Ingresa tu correo electrónico.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Ingresa tu contraseña.");
    }
}

public sealed class RegistroTuristaValidador : AbstractValidator<RegistroTuristaRequest>
{
    public RegistroTuristaValidador()
    {
        RuleFor(x => x.Nombre).NombrePersona("tu nombre");
        RuleFor(x => x.Apellidos).NombrePersona("tus apellidos");
        RuleFor(x => x.Dni).Dni();
        RuleFor(x => x.Correo).CorreoValido();
        RuleFor(x => x.Telefono).Telefono();
        RuleFor(x => x.Password).ContrasenaSegura();
    }
}

public sealed class RegistroEmpresaValidador : AbstractValidator<RegistroEmpresaRequest>
{
    public RegistroEmpresaValidador()
    {
        RuleFor(x => x.Nombre).NombrePersona("el nombre del representante");
        RuleFor(x => x.Apellidos).NombrePersona("los apellidos del representante");
        RuleFor(x => x.Correo).CorreoValido();
        RuleFor(x => x.Telefono).NotEmpty().WithMessage("Ingresa un teléfono de contacto.").Telefono();
        RuleFor(x => x.Password).ContrasenaSegura();
        RuleFor(x => x.RazonSocial).NotEmpty().WithMessage("Ingresa la razón social.")
            .MaximumLength(150).WithMessage("La razón social no puede superar los 150 caracteres.");
        RuleFor(x => x.NombreComercial).NotEmpty().WithMessage("Ingresa el nombre comercial.")
            .MaximumLength(100).WithMessage("El nombre comercial no puede superar los 100 caracteres.");
        RuleFor(x => x.Ruc).Ruc();
        RuleFor(x => x.Direccion).NotEmpty().WithMessage("Ingresa la dirección.")
            .MaximumLength(200).WithMessage("La dirección no puede superar los 200 caracteres.");
    }
}

public sealed class ActualizarPerfilValidador : AbstractValidator<ActualizarPerfilRequest>
{
    public ActualizarPerfilValidador()
    {
        RuleFor(x => x.Nombre).NombrePersona("tu nombre");
        RuleFor(x => x.Apellidos).NombrePersona("tus apellidos");
        RuleFor(x => x.Telefono).Telefono();
        RuleFor(x => x.Dni).Dni().When(x => !string.IsNullOrWhiteSpace(x.Dni));
    }
}

public sealed class CambiarPasswordValidador : AbstractValidator<CambiarPasswordRequest>
{
    public CambiarPasswordValidador()
    {
        RuleFor(x => x.PasswordActual).NotEmpty().WithMessage("Ingresa tu contraseña actual.");
        RuleFor(x => x.PasswordNueva).ContrasenaSegura()
            .NotEqual(x => x.PasswordActual).WithMessage("La nueva contraseña debe ser distinta de la actual.");
    }
}
