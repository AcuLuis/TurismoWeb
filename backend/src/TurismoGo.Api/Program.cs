using Microsoft.EntityFrameworkCore;
using TurismoGo.Domain.CORE.Interfaces;
using TurismoGo.Domain.Infrastructure.Data;
using TurismoGo.Domain.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Cadena de conexión: appsettings.{Entorno}.json, user-secrets o variable de entorno ConnectionStrings__TurismoGo
var cadenaConexion = builder.Configuration.GetConnectionString("TurismoGo")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:TurismoGo'.");
builder.Services.AddDbContext<TurismoDbContext>(options => options.UseSqlServer(cadenaConexion));

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IResenaRepository, ResenaRepository>();
builder.Services.AddScoped<IEmpresaTurismoRepository, EmpresaTurismoRepository>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IRolRepository, RolRepository>();
builder.Services.AddScoped<IActividadRepository, ActividadRepository>();

builder.Services.AddControllers();

// CORS: solo los orígenes configurados (Cors:OrigenesPermitidos)
var origenes = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "TurismoGo API v1"));
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
