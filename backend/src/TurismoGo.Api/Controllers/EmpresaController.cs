using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurismoGo.Application.Actividades;
using TurismoGo.Application.Comun;
using TurismoGo.Application.Empresas;
using TurismoGo.Application.Reservas;
using TurismoGo.Domain;

namespace TurismoGo.Api.Controllers;

/// <summary>Funciones de la empresa de turismo: perfil, actividades y reservas recibidas.</summary>
[ApiController]
[Route("api/empresa")]
[Authorize(Roles = "Empresa")]
public sealed class EmpresaController(
    IEmpresaPerfilService perfil,
    IActividadesEmpresaService actividades,
    IReservasService reservas) : ControllerBase
{
    // ----- Perfil y panel -----

    /// <summary>Perfil de la empresa.</summary>
    [HttpGet("perfil")]
    public Task<EmpresaPerfilDto> Perfil(CancellationToken ct) => perfil.ObtenerAsync(ct);

    /// <summary>Actualiza el perfil de la empresa.</summary>
    [HttpPut("perfil")]
    public Task<EmpresaPerfilDto> ActualizarPerfil(ActualizarEmpresaRequest solicitud, CancellationToken ct) =>
        perfil.ActualizarAsync(solicitud, ct);

    /// <summary>Indicadores del panel de inicio.</summary>
    [HttpGet("panel")]
    public Task<PanelEmpresaDto> Panel(CancellationToken ct) => actividades.PanelAsync(ct);

    // ----- Actividades -----

    /// <summary>Actividades de la empresa.</summary>
    [HttpGet("actividades")]
    public Task<Paginado<ActividadEmpresaFilaDto>> Actividades(
        [FromQuery] string? q, [FromQuery] EstadoActividad? estado, [FromQuery] int? pagina, [FromQuery] int? tamano,
        CancellationToken ct) => actividades.ListarAsync(q, estado, pagina, tamano, ct);

    /// <summary>Datos de una actividad para editarla.</summary>
    [HttpGet("actividades/{id:int}")]
    public Task<ActividadFormularioDto> Actividad(int id, CancellationToken ct) => actividades.ObtenerAsync(id, ct);

    /// <summary>Crea una actividad como borrador.</summary>
    [HttpPost("actividades")]
    [ProducesResponseType<ActividadFormularioDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ActividadFormularioDto>> Crear(GuardarActividadRequest solicitud, CancellationToken ct)
    {
        var id = await actividades.CrearAsync(solicitud, ct);
        return CreatedAtAction(nameof(Actividad), new { id }, await actividades.ObtenerAsync(id, ct));
    }

    /// <summary>Actualiza una actividad.</summary>
    [HttpPut("actividades/{id:int}")]
    public async Task<ActividadFormularioDto> Actualizar(int id, GuardarActividadRequest solicitud, CancellationToken ct)
    {
        await actividades.ActualizarAsync(id, solicitud, ct);
        return await actividades.ObtenerAsync(id, ct);
    }

    /// <summary>Publica una actividad (requiere empresa verificada).</summary>
    [HttpPost("actividades/{id:int}/publicar")]
    public async Task<IActionResult> Publicar(int id, CancellationToken ct)
    {
        await actividades.PublicarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Retira una actividad del catálogo (solo sin reservas activas).</summary>
    [HttpPost("actividades/{id:int}/despublicar")]
    public async Task<IActionResult> Despublicar(int id, CancellationToken ct)
    {
        await actividades.DespublicarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Cancela una actividad y todas sus reservas activas.</summary>
    [HttpPost("actividades/{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id, MotivoRequest solicitud, CancellationToken ct)
    {
        await actividades.CancelarAsync(id, solicitud?.Motivo, ct);
        return NoContent();
    }

    /// <summary>Crea una copia de la actividad como borrador.</summary>
    [HttpPost("actividades/{id:int}/duplicar")]
    public async Task<ActionResult<ActividadFormularioDto>> Duplicar(int id, CancellationToken ct)
    {
        var nuevoId = await actividades.DuplicarAsync(id, ct);
        return CreatedAtAction(nameof(Actividad), new { id = nuevoId }, await actividades.ObtenerAsync(nuevoId, ct));
    }

    /// <summary>Elimina un borrador sin reservas.</summary>
    [HttpDelete("actividades/{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await actividades.EliminarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Lista de participantes en CSV (manifiesto de pasajeros).</summary>
    [HttpGet("actividades/{id:int}/participantes.csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> Participantes(int id, CancellationToken ct)
    {
        var archivo = await actividades.ParticipantesCsvAsync(id, ct);
        return File(archivo.Contenido, archivo.TipoContenido, archivo.Nombre);
    }

    // ----- Reservas recibidas -----

    /// <summary>Reservas recibidas, con filtros.</summary>
    [HttpGet("reservas")]
    public Task<Paginado<ReservaEmpresaDto>> Reservas(
        [FromQuery] int? actividadId, [FromQuery] EstadoReserva? estado, [FromQuery] string? q, [FromQuery] int? pagina,
        CancellationToken ct) => reservas.RecibidasAsync(actividadId, estado, q, pagina, ct);

    /// <summary>Confirma una reserva pendiente.</summary>
    [HttpPost("reservas/{id:int}/confirmar")]
    public async Task<IActionResult> Confirmar(int id, CancellationToken ct)
    {
        await reservas.ConfirmarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Rechaza una reserva pendiente indicando el motivo.</summary>
    [HttpPost("reservas/{id:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, MotivoRequest solicitud, CancellationToken ct)
    {
        await reservas.RechazarAsync(id, solicitud?.Motivo, ct);
        return NoContent();
    }
}
