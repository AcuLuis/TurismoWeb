using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Application.Comun;
using TurismoGo.Domain;

namespace TurismoGo.Application.Empresas;

public sealed record EmpresaPerfilDto(
    int Id, string RazonSocial, string NombreComercial, string Ruc, string Telefono, string Direccion,
    string? Descripcion, string? LogoUrl, EstadoEmpresa Estado, string? MotivoEstado, DateTime? VerificadaEn,
    DateTime CreadoEn, bool PuedeEditarDatosLegales);

public sealed record ActualizarEmpresaRequest(
    string NombreComercial, string Telefono, string Direccion, string? Descripcion, string? LogoUrl,
    string? RazonSocial, string? Ruc);

public sealed class ActualizarEmpresaValidador : AbstractValidator<ActualizarEmpresaRequest>
{
    public ActualizarEmpresaValidador()
    {
        RuleFor(x => x.NombreComercial).NotEmpty().WithMessage("Ingresa el nombre comercial.")
            .MaximumLength(100).WithMessage("El nombre comercial no puede superar los 100 caracteres.");
        RuleFor(x => x.Telefono).NotEmpty().WithMessage("Ingresa un teléfono de contacto.").Telefono();
        RuleFor(x => x.Direccion).NotEmpty().WithMessage("Ingresa la dirección.")
            .MaximumLength(200).WithMessage("La dirección no puede superar los 200 caracteres.");
        RuleFor(x => x.Descripcion).MaximumLength(1000).WithMessage("La descripción no puede superar los 1000 caracteres.");
        RuleFor(x => x.LogoUrl).UrlHttp();
        RuleFor(x => x.RazonSocial).MaximumLength(150).WithMessage("La razón social no puede superar los 150 caracteres.");
        RuleFor(x => x.Ruc!).Ruc().When(x => !string.IsNullOrWhiteSpace(x.Ruc));
    }
}

public interface IEmpresaPerfilService
{
    Task<EmpresaPerfilDto> ObtenerAsync(CancellationToken ct = default);
    Task<EmpresaPerfilDto> ActualizarAsync(ActualizarEmpresaRequest solicitud, CancellationToken ct = default);
}

internal sealed class EmpresaPerfilService(
    IAppDbContext db, IUsuarioActual usuarioActual, IValidator<ActualizarEmpresaRequest> validador) : IEmpresaPerfilService
{
    public async Task<EmpresaPerfilDto> ObtenerAsync(CancellationToken ct = default) => ADto(await EmpresaAsync(ct));

    public async Task<EmpresaPerfilDto> ActualizarAsync(ActualizarEmpresaRequest solicitud, CancellationToken ct = default)
    {
        await validador.ValidarYLanzarAsync(solicitud, ct);
        var empresa = await EmpresaAsync(ct);

        empresa.NombreComercial = solicitud.NombreComercial.Trim();
        empresa.Telefono = solicitud.Telefono.Trim();
        empresa.Direccion = solicitud.Direccion.Trim();
        empresa.Descripcion = Validacion.Limpiar(solicitud.Descripcion);
        empresa.LogoUrl = Validacion.Limpiar(solicitud.LogoUrl);

        // Razón social y RUC solo se pueden corregir mientras la empresa no esté verificada.
        var cambiaLegales = (solicitud.RazonSocial is not null && solicitud.RazonSocial.Trim() != empresa.RazonSocial) ||
                            (solicitud.Ruc is not null && solicitud.Ruc.Trim() != empresa.Ruc);
        if (cambiaLegales)
        {
            if (empresa.Estado != EstadoEmpresa.Pendiente)
                throw new ReglaNegocioException(
                    "La razón social y el RUC no se pueden modificar después de la verificación. Contacta al equipo de TurismoGo.");
            var ruc = solicitud.Ruc?.Trim() ?? empresa.Ruc;
            if (await db.Empresas.AnyAsync(e => e.Ruc == ruc && e.Id != empresa.Id, ct))
                throw new ConflictoException("Ya existe una empresa registrada con ese RUC.");
            empresa.Ruc = ruc;
            if (!string.IsNullOrWhiteSpace(solicitud.RazonSocial)) empresa.RazonSocial = solicitud.RazonSocial.Trim();
        }

        await db.SaveChangesAsync(ct);
        return ADto(empresa);
    }

    private async Task<Domain.Entidades.Empresa> EmpresaAsync(CancellationToken ct)
    {
        var usuarioId = usuarioActual.IdRequerido;
        return await db.Empresas.FirstOrDefaultAsync(e => e.UsuarioId == usuarioId, ct)
               ?? throw new ProhibidoException("Tu cuenta no tiene una empresa asociada.");
    }

    private static EmpresaPerfilDto ADto(Domain.Entidades.Empresa e) => new(
        e.Id, e.RazonSocial, e.NombreComercial, e.Ruc, e.Telefono, e.Direccion, e.Descripcion, e.LogoUrl,
        e.Estado, e.MotivoEstado, e.VerificadaEn, e.CreadoEn, e.Estado == EstadoEmpresa.Pendiente);
}
