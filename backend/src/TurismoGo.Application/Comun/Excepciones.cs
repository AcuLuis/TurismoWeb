namespace TurismoGo.Application.Comun;

/// <summary>Datos de entrada inválidos (400). Contiene los errores por campo en camelCase.</summary>
public sealed class ValidacionException(IDictionary<string, string[]> errores)
    : Exception("Hay datos inválidos.")
{
    public IDictionary<string, string[]> Errores { get; } = errores;

    public static ValidacionException De(string campo, string mensaje) =>
        new(new Dictionary<string, string[]> { [campo] = [mensaje] });
}

/// <summary>El recurso no existe o no es visible para el usuario (404).</summary>
public sealed class NoEncontradoException(string mensaje) : Exception(mensaje);

/// <summary>Conflicto con el estado actual del recurso, como un duplicado (409).</summary>
public sealed class ConflictoException(string mensaje) : Exception(mensaje);

/// <summary>El usuario no tiene permiso para la operación (403).</summary>
public sealed class ProhibidoException(string mensaje) : Exception(mensaje);

/// <summary>Credenciales inválidas o sesión ausente (401).</summary>
public sealed class NoAutorizadoException(string mensaje) : Exception(mensaje);
