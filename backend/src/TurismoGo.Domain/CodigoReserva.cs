using System.Security.Cryptography;

namespace TurismoGo.Domain;

/// <summary>Genera códigos de reserva legibles y no secuenciales (por ejemplo, TG-7KQ2M9XA).</summary>
public static class CodigoReserva
{
    // Sin caracteres ambiguos (0/O, 1/I/L)
    private const string Alfabeto = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    public static string Generar() => "TG-" + RandomNumberGenerator.GetString(Alfabeto, 8);
}
