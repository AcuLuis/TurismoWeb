using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace TurismoGo.Api.Documentacion;

/// <summary>Agrega la información general de la API y el esquema de autenticación Bearer (JWT).</summary>
internal sealed class TransformadorDocumento : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument documento, OpenApiDocumentTransformerContext contexto, CancellationToken ct)
    {
        documento.Info = new OpenApiInfo
        {
            Title = "TurismoGo API",
            Version = "v1",
            Description = "API del marketplace de actividades turísticas TurismoGo. " +
                          "Para probar endpoints protegidos: inicia sesión en /api/auth/login y usa el token en 'Authorize'.",
        };

        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        documento.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token JWT obtenido en /api/auth/login.",
        };
        documento.Security =
        [
            new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", documento)] = [] },
        ];
        return Task.CompletedTask;
    }
}
