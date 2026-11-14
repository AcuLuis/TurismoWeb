using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TurismoGo.Api.Documentacion;
using TurismoGo.Api.Errores;
using TurismoGo.Api.Seguridad;
using TurismoGo.Application;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Application.Comun;
using TurismoGo.Domain;
using TurismoGo.Infrastructure;
using TurismoGo.Infrastructure.Datos;

var builder = WebApplication.CreateBuilder(args);

// ---------- Capas ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<ReglasNegocioOptions>(builder.Configuration.GetSection(ReglasNegocioOptions.Seccion));

// ---------- Autenticación JWT ----------
var jwt = builder.Configuration.GetSection(JwtOptions.Seccion).Get<JwtOptions>() ?? new JwtOptions();
jwt.Validar();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Seccion));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();
builder.Services.AddSingleton<IGeneradorToken, GeneradorTokenJwt>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emisor,
            ValidateAudience = true,
            ValidAudience = jwt.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwt.ObtenerClave(),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = Claims.Nombre,
            RoleClaimType = Claims.Rol,
        };
        o.Events = new JwtBearerEvents
        {
            // Una cuenta suspendida (o con rol cambiado) pierde el acceso de inmediato, aunque su token siga vigente.
            OnTokenValidated = async ctx =>
            {
                var principal = ctx.Principal!;
                if (!int.TryParse(principal.FindFirst(Claims.Id)?.Value, out var id) ||
                    !Enum.TryParse<Rol>(principal.FindFirst(Claims.Rol)?.Value, out var rol))
                {
                    ctx.Fail("Token inválido.");
                    return;
                }
                var db = ctx.HttpContext.RequestServices.GetRequiredService<IAppDbContext>();
                var vigente = await db.Usuarios.AnyAsync(u => u.Id == id && u.Activo && u.Rol == rol, ctx.HttpContext.RequestAborted);
                if (!vigente) ctx.Fail("La cuenta no está activa.");
            },
        };
    });
builder.Services.AddAuthorization();

// ---------- Limitación de intentos (login/registro) ----------
var intentosPorMinuto = builder.Configuration.GetValue("RateLimiting:IntentosAuthPorMinuto", 10);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(Program.PoliticaAuth, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = intentosPorMinuto, Window = TimeSpan.FromMinutes(1) }));
});

// ---------- MVC, JSON y errores ----------
builder.Services
    .AddControllers(o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = ctx =>
    {
        // Errores de formato (JSON mal formado o tipos incorrectos): no se exponen los mensajes internos del parser.
        var errores = ctx.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .GroupBy(e => NormalizarCampo(e.Key))
            .ToDictionary(g => g.Key, _ => MensajeFormatoInvalido);
        var problema = new ValidationProblemDetails(errores)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Hay datos inválidos.",
            Detail = "La solicitud tiene un formato incorrecto o valores con un tipo no válido.",
        };
        problema.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(problema);

        static string NormalizarCampo(string clave)
        {
            var c = clave.TrimStart('$').TrimStart('.');
            return string.IsNullOrEmpty(c) ? "cuerpo" : char.ToLowerInvariant(c[0]) + c[1..];
        }
    });

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
{
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    ctx.ProblemDetails.Type = null;
    if (ctx.ProblemDetails.Detail is not null) return;
    (ctx.ProblemDetails.Title, ctx.ProblemDetails.Detail) = ctx.ProblemDetails.Status switch
    {
        401 => ("No autorizado", "Inicia sesión para continuar."),
        403 => ("Acceso denegado", "No tienes permiso para realizar esta acción."),
        404 => ("No encontrado", "El recurso solicitado no existe."),
        405 => ("Método no permitido", "La operación no está disponible para este recurso."),
        429 => ("Demasiados intentos", "Has realizado demasiados intentos. Espera un minuto y vuelve a intentarlo."),
        _ => (ctx.ProblemDetails.Title, ctx.ProblemDetails.Detail),
    };
});
builder.Services.AddExceptionHandler<ManejadorExcepciones>();

// ---------- CORS ----------
var origenes = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod()
    .WithExposedHeaders("Content-Disposition")));

// ---------- Documentación OpenAPI ----------
builder.Services.AddOpenApi(o => o.AddDocumentTransformer<TransformadorDocumento>());

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h.XContentTypeOptions = "nosniff";
    h.XFrameOptions = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "TurismoGo API v1");
        o.DocumentTitle = "TurismoGo API";
    });
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health");

// Migraciones, administrador inicial y datos demo (según la sección "Inicializacion").
// Con todo deshabilitado no accede a la base de datos.
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<InicializadorDatos>().EjecutarAsync();
}

await app.RunAsync();

public partial class Program
{
    public const string PoliticaAuth = "auth";
    private static readonly string[] MensajeFormatoInvalido = ["El valor no tiene un formato válido."];
}
