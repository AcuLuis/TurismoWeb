using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TurismoGo.Application.Cuentas;

namespace TurismoGo.Api.Controllers;

/// <summary>Inicio de sesión y registro de cuentas.</summary>
[ApiController]
[Route("api/auth")]
[EnableRateLimiting(Program.PoliticaAuth)]
public sealed class AuthController(ICuentasService cuentas) : ControllerBase
{
    /// <summary>Inicia sesión y devuelve un token JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<SesionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public Task<SesionDto> Login(LoginRequest solicitud, CancellationToken ct) => cuentas.LoginAsync(solicitud, ct);

    /// <summary>Registra un turista e inicia su sesión.</summary>
    [HttpPost("registro/turista")]
    [AllowAnonymous]
    [ProducesResponseType<SesionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SesionDto>> RegistrarTurista(RegistroTuristaRequest solicitud, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await cuentas.RegistrarTuristaAsync(solicitud, ct));

    /// <summary>Registra una empresa de turismo (queda pendiente de verificación) e inicia su sesión.</summary>
    [HttpPost("registro/empresa")]
    [AllowAnonymous]
    [ProducesResponseType<SesionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SesionDto>> RegistrarEmpresa(RegistroEmpresaRequest solicitud, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await cuentas.RegistrarEmpresaAsync(solicitud, ct));

    /// <summary>Datos del usuario de la sesión actual.</summary>
    [HttpGet("me")]
    [Authorize]
    [DisableRateLimiting]
    public Task<UsuarioSesionDto> Me(CancellationToken ct) => cuentas.ObtenerSesionAsync(ct);
}

/// <summary>Gestión de la cuenta propia.</summary>
[ApiController]
[Route("api/cuenta")]
[Authorize]
public sealed class CuentaController(ICuentasService cuentas) : ControllerBase
{
    /// <summary>Actualiza los datos personales.</summary>
    [HttpPut("perfil")]
    public Task<UsuarioSesionDto> ActualizarPerfil(ActualizarPerfilRequest solicitud, CancellationToken ct) =>
        cuentas.ActualizarPerfilAsync(solicitud, ct);

    /// <summary>Cambia la contraseña (requiere la contraseña actual).</summary>
    [HttpPut("password")]
    [EnableRateLimiting(Program.PoliticaAuth)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordRequest solicitud, CancellationToken ct)
    {
        await cuentas.CambiarPasswordAsync(solicitud, ct);
        return NoContent();
    }
}
