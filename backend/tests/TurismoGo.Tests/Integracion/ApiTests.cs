using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Infrastructure.Datos;

namespace TurismoGo.Tests.Integracion;

/// <summary>API completa en memoria: SQLite compartido + datos demo.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conexion = new("DataSource=:memory:");

    public ApiFactory()
    {
        // Se definen antes de que Program lea la configuración
        Environment.SetEnvironmentVariable("Jwt__Clave", "clave-exclusiva-de-pruebas-0123456789-abcdef");
        Environment.SetEnvironmentVariable("RateLimiting__IntentosAuthPorMinuto", "10000");
        _conexion.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(s => s.AddDbContext<TurismoDbContext>(o => o.UseSqlite(_conexion)));
    }

    public async Task InicializarAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TurismoDbContext>();
        if (await db.Database.EnsureCreatedAsync())
        {
            var sp = scope.ServiceProvider;
            await new DatosDemo(db, sp.GetRequiredService<IHasherContrasenas>(), sp.GetRequiredService<IReloj>()).SembrarAsync(default);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _conexion.Dispose();
    }
}

public sealed class ApiTests(ApiFactory fabrica) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _cliente = fabrica.CreateClient();

    public Task InitializeAsync() => fabrica.InicializarAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<string> TokenAsync(string correo)
    {
        var r = await _cliente.PostAsJsonAsync("/api/auth/login", new { correo, password = DatosDemo.PasswordDemo });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("token").GetString()!;
    }

    private static HttpRequestMessage Peticion(HttpMethod metodo, string url, string? token, object? cuerpo = null)
    {
        var p = new HttpRequestMessage(metodo, url);
        if (token is not null) p.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (cuerpo is not null) p.Content = JsonContent.Create(cuerpo);
        return p;
    }

    [Fact]
    public async Task Health_responde_ok()
    {
        var r = await _cliente.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Catalogo_es_publico_y_no_expone_datos_sensibles()
    {
        var r = await _cliente.GetAsync("/api/actividades?tamano=50");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var texto = await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contrasena", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ruc", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_nunca_devuelve_la_contrasena_ni_el_hash()
    {
        var r = await _cliente.PostAsJsonAsync("/api/auth/login", new { correo = "ana.quispe@correo.pe", password = DatosDemo.PasswordDemo });
        var texto = await r.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.DoesNotContain(DatosDemo.PasswordDemo, texto);
        Assert.DoesNotContain("passwordHash", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("GET", "/api/mis/reservas")]
    [InlineData("GET", "/api/empresa/panel")]
    [InlineData("GET", "/api/admin/resumen")]
    [InlineData("GET", "/api/auth/me")]
    public async Task Endpoints_protegidos_exigen_sesion(string metodo, string url)
    {
        var r = await _cliente.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), url));
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        var problema = await r.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("Inicia sesión para continuar.", problema.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Roles_se_respetan()
    {
        var turista = await TokenAsync("ana.quispe@correo.pe");
        var empresa = await TokenAsync("ventas@andesexplorer.pe");

        Assert.Equal(HttpStatusCode.Forbidden, (await _cliente.SendAsync(Peticion(HttpMethod.Get, "/api/empresa/panel", turista))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _cliente.SendAsync(Peticion(HttpMethod.Get, "/api/admin/resumen", empresa))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _cliente.SendAsync(Peticion(HttpMethod.Post, "/api/reservas", empresa, new { actividadId = 1, cantidadPersonas = 1 }))).StatusCode);
    }

    [Fact]
    public async Task Token_manipulado_es_rechazado()
    {
        var token = await TokenAsync("ana.quispe@correo.pe");
        var partes = token.Split('.');
        var falso = $"{partes[0]}.{partes[1]}.firmaInvalida";
        var r = await _cliente.SendAsync(Peticion(HttpMethod.Get, "/api/auth/me", falso));
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Errores_son_problem_details_sin_trazas()
    {
        var r = await _cliente.GetAsync("/api/actividades/999999");
        var texto = await r.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("Exception", texto);
        Assert.DoesNotContain("   at ", texto);
    }

    [Fact]
    public async Task Json_malformado_devuelve_400_sin_detalles_internos()
    {
        var r = await _cliente.PostAsync("/api/auth/login", new StringContent("{\"correo\":", System.Text.Encoding.UTF8, "application/json"));
        var texto = await r.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.DoesNotContain("BytePositionInLine", texto);
    }

    [Fact]
    public async Task Flujo_completo_de_reserva_y_confirmacion()
    {
        // Un turista nuevo se registra y reserva
        var registro = await _cliente.PostAsJsonAsync("/api/auth/registro/turista", new
        {
            nombre = "Prueba", apellidos = "Integración", dni = "70000001", correo = "flujo@correo.pe", password = "Segura2026",
        });
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);
        var turista = (await registro.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("token").GetString()!;

        var catalogo = await _cliente.GetFromJsonAsync<JsonElement>("/api/actividades?q=Colca", Json);
        var actividadId = catalogo.GetProperty("items")[0].GetProperty("id").GetInt32();

        var reserva = await _cliente.SendAsync(Peticion(HttpMethod.Post, "/api/reservas", turista, new { actividadId, cantidadPersonas = 2 }));
        Assert.Equal(HttpStatusCode.Created, reserva.StatusCode);
        var reservaJson = await reserva.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("Pendiente", reservaJson.GetProperty("estado").GetString());

        // La empresa la ve y la confirma
        var empresa = await TokenAsync("ventas@andesexplorer.pe");
        var recibidas = await (await _cliente.SendAsync(Peticion(HttpMethod.Get, $"/api/empresa/reservas?actividadId={actividadId}&estado=Pendiente", empresa)))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var id = recibidas.GetProperty("items").EnumerateArray()
            .First(r => r.GetProperty("codigo").GetString() == reservaJson.GetProperty("codigo").GetString()).GetProperty("id").GetInt32();
        var confirmar = await _cliente.SendAsync(Peticion(HttpMethod.Post, $"/api/empresa/reservas/{id}/confirmar", empresa));
        Assert.Equal(HttpStatusCode.NoContent, confirmar.StatusCode);

        // El turista la ve confirmada
        var mias = await (await _cliente.SendAsync(Peticion(HttpMethod.Get, "/api/mis/reservas?filtro=proximas", turista)))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("Confirmada", mias.GetProperty("items")[0].GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Csv_de_participantes_se_descarga_para_la_empresa_duena()
    {
        var empresa = await TokenAsync("ventas@andesexplorer.pe");
        var actividades = await (await _cliente.SendAsync(Peticion(HttpMethod.Get, "/api/empresa/actividades?q=Camino", empresa)))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var id = actividades.GetProperty("items")[0].GetProperty("id").GetInt32();

        var r = await _cliente.SendAsync(Peticion(HttpMethod.Get, $"/api/empresa/actividades/{id}/participantes.csv", empresa));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("text/csv", r.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Código;Apellidos", await r.Content.ReadAsStringAsync());

        var otra = await TokenAsync("reservas@costaviva.pe");
        var ajena = await _cliente.SendAsync(Peticion(HttpMethod.Get, $"/api/empresa/actividades/{id}/participantes.csv", otra));
        Assert.Equal(HttpStatusCode.NotFound, ajena.StatusCode);
    }
}
