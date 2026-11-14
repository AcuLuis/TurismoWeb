using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurismoGo.Application.Comun;
using TurismoGo.Domain;

namespace TurismoGo.Api.Errores;

/// <summary>
/// Convierte las excepciones en respuestas ProblemDetails (RFC 9457) con mensajes en español.
/// Nunca expone trazas ni detalles internos: los errores inesperados se registran en el log.
/// </summary>
public sealed partial class ManejadorExcepciones(IProblemDetailsService problemDetails, ILogger<ManejadorExcepciones> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var http = httpContext;
        ProblemDetails detalle = exception switch
        {
            ValidacionException v => new ValidationProblemDetails(v.Errores)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Hay datos inválidos.",
                Detail = "Revisa los campos marcados.",
            },
            NoEncontradoException e => Crear(StatusCodes.Status404NotFound, "No encontrado", e.Message),
            ConflictoException e => Crear(StatusCodes.Status409Conflict, "Conflicto", e.Message),
            ReglaNegocioException e => Crear(StatusCodes.Status422UnprocessableEntity, "Operación no permitida", e.Message),
            ProhibidoException e => Crear(StatusCodes.Status403Forbidden, "Acceso denegado", e.Message),
            NoAutorizadoException e => Crear(StatusCodes.Status401Unauthorized, "No autorizado", e.Message),
            DbUpdateConcurrencyException => Crear(StatusCodes.Status409Conflict, "Conflicto",
                "Los datos cambiaron mientras realizabas la operación. Vuelve a intentarlo."),
            BadHttpRequestException e => Crear(e.StatusCode, "Solicitud inválida", "La solicitud no tiene un formato válido."),
            OperationCanceledException => Crear(499, "Solicitud cancelada", "La solicitud fue cancelada."),
            _ => Crear(StatusCodes.Status500InternalServerError, "Error inesperado",
                "Ocurrió un error inesperado. Inténtalo nuevamente en unos minutos."),
        };

        if (detalle.Status >= 500)
            LogErrorNoControlado(logger, exception, http.Request.Method, http.Request.Path);

        http.Response.StatusCode = detalle.Status ?? 500;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = detalle,
            Exception = exception,
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error no controlado en {Metodo} {Ruta}")]
    private static partial void LogErrorNoControlado(ILogger logger, Exception excepcion, string metodo, PathString ruta);

    private static ProblemDetails Crear(int estado, string titulo, string detalle) =>
        new() { Status = estado, Title = titulo, Detail = detalle };
}
