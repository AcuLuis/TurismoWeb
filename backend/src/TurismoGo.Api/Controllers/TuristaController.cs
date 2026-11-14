using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurismoGo.Application.Actividades;
using TurismoGo.Application.Comun;
using TurismoGo.Application.Reservas;

namespace TurismoGo.Api.Controllers;

/// <summary>Reservas y reseñas del turista.</summary>
[ApiController]
[Route("api")]
[Authorize(Roles = "Turista")]
public sealed class TuristaController(IReservasService reservas) : ControllerBase
{
    /// <summary>Crea una reserva (queda pendiente hasta que la empresa la confirme).</summary>
    [HttpPost("reservas")]
    [ProducesResponseType<ReservaTuristaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservaTuristaDto>> Crear(CrearReservaRequest solicitud, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await reservas.CrearAsync(solicitud, ct));

    /// <summary>Reservas del turista. Filtro: proximas | pasadas | canceladas.</summary>
    [HttpGet("mis/reservas")]
    public Task<Paginado<ReservaTuristaDto>> MisReservas([FromQuery] string? filtro, [FromQuery] int? pagina, CancellationToken ct) =>
        reservas.MisReservasAsync(filtro, pagina, ct);

    /// <summary>Cancela una reserva propia (según la política de cancelación).</summary>
    [HttpPost("reservas/{id:int}/cancelar")]
    public Task<ReservaTuristaDto> Cancelar(int id, MotivoRequest? solicitud, CancellationToken ct) =>
        reservas.CancelarAsync(id, solicitud?.Motivo, ct);

    /// <summary>Publica una reseña de una actividad completada.</summary>
    [HttpPost("actividades/{id:int}/resenas")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Resenar(int id, CrearResenaRequest solicitud, CancellationToken ct)
    {
        await reservas.CrearResenaAsync(id, solicitud, ct);
        return StatusCode(StatusCodes.Status201Created);
    }
}
