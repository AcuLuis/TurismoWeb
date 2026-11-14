using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurismoGo.Application.Actividades;
using TurismoGo.Application.Administracion;
using TurismoGo.Application.Comun;
using TurismoGo.Domain;

namespace TurismoGo.Api.Controllers;

/// <summary>Administración de la plataforma: verificación de empresas, usuarios, moderación y reportes.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController(IAdminService admin) : ControllerBase
{
    /// <summary>Indicadores y reportes generales.</summary>
    [HttpGet("resumen")]
    public Task<AdminResumenDto> Resumen(CancellationToken ct) => admin.ResumenAsync(ct);

    /// <summary>Empresas registradas (las pendientes de verificación aparecen primero).</summary>
    [HttpGet("empresas")]
    public Task<Paginado<EmpresaAdminDto>> Empresas(
        [FromQuery] EstadoEmpresa? estado, [FromQuery] string? q, [FromQuery] int? pagina, CancellationToken ct) =>
        admin.EmpresasAsync(estado, q, pagina, ct);

    /// <summary>Verifica una empresa (podrá publicar actividades).</summary>
    [HttpPost("empresas/{id:int}/verificar")]
    public async Task<IActionResult> VerificarEmpresa(int id, CancellationToken ct)
    {
        await admin.VerificarEmpresaAsync(id, ct);
        return NoContent();
    }

    /// <summary>Suspende una empresa: sus actividades dejan de mostrarse.</summary>
    [HttpPost("empresas/{id:int}/suspender")]
    public async Task<IActionResult> SuspenderEmpresa(int id, MotivoRequest solicitud, CancellationToken ct)
    {
        await admin.SuspenderEmpresaAsync(id, solicitud?.Motivo, ct);
        return NoContent();
    }

    /// <summary>Usuarios de la plataforma.</summary>
    [HttpGet("usuarios")]
    public Task<Paginado<UsuarioAdminDto>> Usuarios(
        [FromQuery] Rol? rol, [FromQuery] string? q, [FromQuery] int? pagina, CancellationToken ct) =>
        admin.UsuariosAsync(rol, q, pagina, ct);

    /// <summary>Suspende una cuenta (pierde el acceso de inmediato).</summary>
    [HttpPost("usuarios/{id:int}/suspender")]
    public async Task<IActionResult> SuspenderUsuario(int id, CancellationToken ct)
    {
        await admin.CambiarEstadoUsuarioAsync(id, activo: false, ct);
        return NoContent();
    }

    /// <summary>Reactiva una cuenta suspendida.</summary>
    [HttpPost("usuarios/{id:int}/reactivar")]
    public async Task<IActionResult> ReactivarUsuario(int id, CancellationToken ct)
    {
        await admin.CambiarEstadoUsuarioAsync(id, activo: true, ct);
        return NoContent();
    }

    /// <summary>Reseñas para moderación (las de menor valoración primero).</summary>
    [HttpGet("resenas")]
    public Task<Paginado<ResenaAdminDto>> Resenas(
        [FromQuery] bool? oculta, [FromQuery] string? q, [FromQuery] int? pagina, CancellationToken ct) =>
        admin.ResenasAsync(oculta, q, pagina, ct);

    /// <summary>Oculta una reseña indicando el motivo.</summary>
    [HttpPost("resenas/{id:int}/ocultar")]
    public async Task<IActionResult> OcultarResena(int id, MotivoRequest solicitud, CancellationToken ct)
    {
        await admin.OcultarResenaAsync(id, solicitud?.Motivo, ct);
        return NoContent();
    }

    /// <summary>Vuelve a mostrar una reseña oculta.</summary>
    [HttpPost("resenas/{id:int}/mostrar")]
    public async Task<IActionResult> MostrarResena(int id, CancellationToken ct)
    {
        await admin.MostrarResenaAsync(id, ct);
        return NoContent();
    }
}
