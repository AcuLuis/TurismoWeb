namespace TurismoGo.Domain;

/// <summary>
/// Se lanza cuando una operación viola una regla del negocio
/// (por ejemplo, reservar más personas que los cupos disponibles).
/// </summary>
public sealed class ReglaNegocioException(string mensaje) : Exception(mensaje);
