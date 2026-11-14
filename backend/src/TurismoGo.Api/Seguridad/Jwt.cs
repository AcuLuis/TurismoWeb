using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Application.Comun;
using TurismoGo.Domain;
using TurismoGo.Domain.Entidades;

namespace TurismoGo.Api.Seguridad;

/// <summary>Sección "Jwt" de appsettings.</summary>
public sealed class JwtOptions
{
    public const string Seccion = "Jwt";

    public string Emisor { get; set; } = "TurismoGo";
    public string Audiencia { get; set; } = "TurismoGo.Web";

    /// <summary>Clave HMAC de al menos 32 caracteres. En producción se define por variable de entorno (Jwt__Clave).</summary>
    public string Clave { get; set; } = string.Empty;

    public int MinutosExpiracion { get; set; } = 480;

    public SymmetricSecurityKey ObtenerClave() => new(Encoding.UTF8.GetBytes(Clave));

    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Clave) || Encoding.UTF8.GetByteCount(Clave) < 32)
            throw new InvalidOperationException(
                "Configura 'Jwt:Clave' con al menos 32 caracteres (variable de entorno Jwt__Clave).");
    }
}

public static class Claims
{
    public const string Id = "sub";
    public const string Rol = "role";
    public const string Nombre = "name";
    public const string Correo = "email";
}

public sealed class GeneradorTokenJwt(IOptions<JwtOptions> opciones, IReloj reloj) : IGeneradorToken
{
    private readonly JsonWebTokenHandler _handler = new();

    public TokenGenerado Generar(Usuario usuario)
    {
        var o = opciones.Value;
        var expira = reloj.UtcAhora.AddMinutes(o.MinutosExpiracion);
        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = o.Emisor,
            Audience = o.Audiencia,
            IssuedAt = reloj.UtcAhora,
            NotBefore = reloj.UtcAhora,
            Expires = expira,
            Subject = new ClaimsIdentity(
            [
                new Claim(Claims.Id, usuario.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim(Claims.Correo, usuario.Correo),
                new Claim(Claims.Nombre, usuario.NombreCompleto),
                new Claim(Claims.Rol, usuario.Rol.ToString()),
            ]),
            SigningCredentials = new SigningCredentials(o.ObtenerClave(), SecurityAlgorithms.HmacSha256),
        });
        return new TokenGenerado(token, expira);
    }
}

public sealed class UsuarioActual(IHttpContextAccessor accesor) : IUsuarioActual
{
    private ClaimsPrincipal? Principal => accesor.HttpContext?.User;

    public int? Id => int.TryParse(Principal?.FindFirstValue(Claims.Id), out var id) ? id : null;

    public Rol? Rol => Enum.TryParse<Rol>(Principal?.FindFirstValue(Claims.Rol), out var rol) ? rol : null;

    public int IdRequerido => Id ?? throw new NoAutorizadoException("Inicia sesión para continuar.");
}
