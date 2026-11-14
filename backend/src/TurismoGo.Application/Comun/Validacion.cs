using System.Text.RegularExpressions;
using FluentValidation;

namespace TurismoGo.Application.Comun;

public static partial class Validacion
{
    /// <summary>Valida y lanza <see cref="ValidacionException"/> con claves en camelCase si hay errores.</summary>
    public static async Task ValidarYLanzarAsync<T>(this IValidator<T> validador, T instancia, CancellationToken ct = default)
    {
        if (instancia is null)
            throw ValidacionException.De("cuerpo", "La solicitud no tiene contenido.");

        var resultado = await validador.ValidateAsync(instancia, ct);
        if (resultado.IsValid) return;

        var errores = resultado.Errors
            .GroupBy(e => ACamelCase(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        throw new ValidacionException(errores);
    }

    /// <summary>"Itinerario[0].Titulo" → "itinerario[0].titulo".</summary>
    internal static string ACamelCase(string ruta) =>
        string.Join('.', ruta.Split('.').Select(s => s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s[1..]));

    // --- Reglas reutilizables ---

    public static IRuleBuilderOptions<T, string> ContrasenaSegura<T>(this IRuleBuilder<T, string> regla) =>
        regla.NotEmpty().WithMessage("Ingresa una contraseña.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(100).WithMessage("La contraseña no puede superar los 100 caracteres.")
            .Must(p => p is not null && p.Any(char.IsLetter) && p.Any(char.IsDigit))
            .WithMessage("La contraseña debe incluir letras y números.");

    public static IRuleBuilderOptions<T, string> CorreoValido<T>(this IRuleBuilder<T, string> regla) =>
        regla.NotEmpty().WithMessage("Ingresa tu correo electrónico.")
            .MaximumLength(100).WithMessage("El correo no puede superar los 100 caracteres.")
            .Must(c => c is not null && RegexCorreo().IsMatch(c.Trim())).WithMessage("Ingresa un correo electrónico válido.");

    public static IRuleBuilderOptions<T, string?> Dni<T>(this IRuleBuilder<T, string?> regla) =>
        regla.Must(d => d is not null && RegexDni().IsMatch(d.Trim())).WithMessage("El DNI debe tener 8 dígitos.");

    public static IRuleBuilderOptions<T, string> Ruc<T>(this IRuleBuilder<T, string> regla) =>
        regla.NotEmpty().WithMessage("Ingresa el RUC.")
            .Must(r => r is not null && RegexRuc().IsMatch(r.Trim()))
            .WithMessage("El RUC debe tener 11 dígitos y empezar por 10, 15, 17 o 20.");

    public static IRuleBuilderOptions<T, string?> Telefono<T>(this IRuleBuilder<T, string?> regla) =>
        regla.Must(t => t is null || RegexTelefono().IsMatch(t.Trim()))
            .WithMessage("Ingresa un teléfono válido (de 7 a 15 dígitos, puede empezar con +).");

    public static IRuleBuilderOptions<T, string> NombrePersona<T>(this IRuleBuilder<T, string> regla, string campo) =>
        regla.NotEmpty().WithMessage($"Ingresa {campo}.")
            .MaximumLength(100).WithMessage($"El campo {campo} no puede superar los 100 caracteres.");

    public static IRuleBuilderOptions<T, string?> UrlHttp<T>(this IRuleBuilder<T, string?> regla) =>
        regla.Must(u => string.IsNullOrWhiteSpace(u) ||
                        (u.Length <= 500 && Uri.TryCreate(u, UriKind.Absolute, out var uri) &&
                         (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)))
            .WithMessage("Ingresa una URL válida que empiece con http:// o https://.");

    public static string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();

    public static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$")]
    private static partial Regex RegexCorreo();

    [GeneratedRegex(@"^\d{8}$")]
    private static partial Regex RegexDni();

    [GeneratedRegex(@"^(10|15|17|20)\d{9}$")]
    private static partial Regex RegexRuc();

    [GeneratedRegex(@"^\+?\d{7,15}$")]
    private static partial Regex RegexTelefono();
}
