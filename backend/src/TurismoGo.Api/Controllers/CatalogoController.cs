using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurismoGo.Application.Actividades;
using TurismoGo.Application.Comun;

namespace TurismoGo.Api.Controllers;

/// <summary>Catálogo público de actividades. No requiere sesión.</summary>
[ApiController]
[Route("api")]
[AllowAnonymous]
public sealed class CatalogoController(ICatalogoService catalogo) : ControllerBase
{
    /// <summary>Busca actividades próximas con filtros, orden y paginación.</summary>
    /// <param name="q">Texto a buscar en nombre, descripción, destino o empresa.</param>
    /// <param name="destino">Destino exacto (ver /api/destinos).</param>
    /// <param name="desde">Fecha mínima de inicio (yyyy-MM-dd).</param>
    /// <param name="hasta">Fecha máxima de inicio (yyyy-MM-dd).</param>
    /// <param name="precioMin">Precio mínimo por persona (S/).</param>
    /// <param name="precioMax">Precio máximo por persona (S/).</param>
    /// <param name="orden">fecha | precio-asc | precio-desc | valoracion</param>
    /// <param name="pagina">Número de página (desde 1).</param>
    /// <param name="tamano">Elementos por página (máximo 50).</param>
    /// <param name="ct">Cancelación.</param>
    [HttpGet("actividades")]
    public Task<Paginado<ActividadResumenDto>> Buscar(
        [FromQuery] string? q, [FromQuery] string? destino, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
        [FromQuery] decimal? precioMin, [FromQuery] decimal? precioMax, [FromQuery] string? orden,
        [FromQuery] int? pagina, [FromQuery] int? tamano, CancellationToken ct) =>
        catalogo.BuscarAsync(new FiltroCatalogo(q, destino, desde, hasta, precioMin, precioMax, orden, pagina, tamano), ct);

    /// <summary>Detalle de una actividad: itinerario, empresa y valoraciones.</summary>
    [HttpGet("actividades/{id:int}")]
    [ProducesResponseType<ActividadDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ActividadDetalleDto> Detalle(int id, CancellationToken ct) => catalogo.DetalleAsync(id, ct);

    /// <summary>Reseñas visibles de una actividad, con promedio y distribución.</summary>
    [HttpGet("actividades/{id:int}/resenas")]
    public Task<ResenasActividadDto> Resenas(int id, [FromQuery] int? pagina, CancellationToken ct) =>
        catalogo.ResenasAsync(id, pagina, ct);

    /// <summary>Destinos con actividades disponibles.</summary>
    [HttpGet("destinos")]
    public Task<IReadOnlyList<string>> Destinos(CancellationToken ct) => catalogo.DestinosAsync(ct);
}
