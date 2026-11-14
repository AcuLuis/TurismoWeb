using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Application.Actividades;
using TurismoGo.Application.Comun;
using TurismoGo.Application.Cuentas;
using TurismoGo.Application.Reservas;
using TurismoGo.Domain;
using TurismoGo.Domain.Entidades;
using TurismoGo.Tests.Infra;

namespace TurismoGo.Tests.Servicios;

file sealed class TokenFalso : IGeneradorToken
{
    public TokenGenerado Generar(Usuario usuario) => new($"token-{usuario.Id}", DateTime.UtcNow.AddHours(1));
}

public sealed class CuentasServiceTests : IDisposable
{
    private readonly BaseDatosPrueba _bd = new();

    private CuentasService Servicio(int? usuarioId = null) => new(
        _bd.Db, Hasher.Instancia, new TokenFalso(), new UsuarioActualFijo(usuarioId, null), _bd.Reloj,
        new LoginValidador(), new RegistroTuristaValidador(), new RegistroEmpresaValidador(),
        new ActualizarPerfilValidador(), new CambiarPasswordValidador());

    private static RegistroTuristaRequest Turista(string correo = "Ana@Correo.PE", string dni = "45678912") =>
        new("Ana", "Quispe", dni, correo, null, "Segura2026");

    [Fact]
    public async Task Registro_guarda_hash_y_normaliza_correo()
    {
        var sesion = await Servicio().RegistrarTuristaAsync(Turista());

        var u = await _bd.Db.Usuarios.SingleAsync();
        Assert.Equal("ana@correo.pe", u.Correo);
        Assert.NotEqual("Segura2026", u.PasswordHash);
        Assert.True(Hasher.Instancia.Verificar(u.PasswordHash, "Segura2026"));
        Assert.Equal(Rol.Turista, sesion.Usuario.Rol);
    }

    [Fact]
    public async Task Registro_rechaza_correo_y_dni_duplicados()
    {
        await Servicio().RegistrarTuristaAsync(Turista());

        await Assert.ThrowsAsync<ConflictoException>(() => Servicio().RegistrarTuristaAsync(Turista(dni: "11111111")));
        await Assert.ThrowsAsync<ConflictoException>(() => Servicio().RegistrarTuristaAsync(Turista(correo: "otra@correo.pe")));
    }

    [Fact]
    public async Task Registro_valida_campos_con_mensajes_en_espanol_y_claves_camelCase()
    {
        var ex = await Assert.ThrowsAsync<ValidacionException>(() =>
            Servicio().RegistrarTuristaAsync(new RegistroTuristaRequest("", "Q", "12", "no-es-correo", "abc", "corta")));

        Assert.Contains("nombre", ex.Errores.Keys);
        Assert.Contains("dni", ex.Errores.Keys);
        Assert.Contains("correo", ex.Errores.Keys);
        Assert.Contains("telefono", ex.Errores.Keys);
        Assert.Contains("password", ex.Errores.Keys);
        Assert.Equal("El DNI debe tener 8 dígitos.", ex.Errores["dni"][0]);
    }

    [Fact]
    public async Task Empresa_nueva_queda_pendiente_de_verificacion()
    {
        var sesion = await Servicio().RegistrarEmpresaAsync(new RegistroEmpresaRequest(
            "Rosa", "Huamán", "ventas@andes.pe", "984112233", "Segura2026", "Andes SAC", "Andes", "20601234567", "Cusco"));

        Assert.Equal(EstadoEmpresa.Pendiente, sesion.Usuario.Empresa!.Estado);
    }

    [Fact]
    public async Task Login_con_credenciales_incorrectas_no_revela_si_el_correo_existe()
    {
        await Servicio().RegistrarTuristaAsync(Turista());

        var e1 = await Assert.ThrowsAsync<NoAutorizadoException>(() => Servicio().LoginAsync(new("ana@correo.pe", "mala")));
        var e2 = await Assert.ThrowsAsync<NoAutorizadoException>(() => Servicio().LoginAsync(new("nadie@correo.pe", "mala")));
        Assert.Equal(e1.Message, e2.Message);
    }

    [Fact]
    public async Task Login_de_cuenta_suspendida_es_prohibido()
    {
        await Servicio().RegistrarTuristaAsync(Turista());
        var u = await _bd.Db.Usuarios.SingleAsync();
        u.Activo = false;
        await _bd.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<ProhibidoException>(() => Servicio().LoginAsync(new("ana@correo.pe", "Segura2026")));
    }

    public void Dispose() => _bd.Dispose();
}

public sealed class ReservasServiceTests : IDisposable
{
    private readonly BaseDatosPrueba _bd = new();
    private static readonly IOptions<ReglasNegocioOptions> Reglas = Options.Create(new ReglasNegocioOptions());

    private ReservasService Servicio(int usuarioId, IAppDbContext? db = null) => new(
        db ?? _bd.Db, _bd.Reloj, new UsuarioActualFijo(usuarioId, Rol.Turista), Reglas,
        new CrearReservaValidador(Reglas), new CrearResenaValidador());

    [Fact]
    public async Task Reserva_calcula_total_en_servidor_y_aparta_cupos()
    {
        var a = _bd.CrearActividad(_bd.CrearEmpresa(), capacidad: 5, precio: 120.50m);
        var t = _bd.CrearTurista();

        var r = await Servicio(t.Id).CrearAsync(new CrearReservaRequest(a.Id, 2, null));

        Assert.Equal(241.00m, r.Total);
        Assert.Equal(EstadoReservaVista.Pendiente, r.Estado);
        Assert.Equal(2, (await _bd.NuevoContexto().Actividades.FindAsync(a.Id))!.CuposReservados);
    }

    [Fact]
    public async Task No_permite_dos_reservas_activas_de_la_misma_actividad()
    {
        var a = _bd.CrearActividad(_bd.CrearEmpresa());
        var t = _bd.CrearTurista();
        await Servicio(t.Id).CrearAsync(new CrearReservaRequest(a.Id, 1, null));

        await Assert.ThrowsAsync<ConflictoException>(() => Servicio(t.Id).CrearAsync(new CrearReservaRequest(a.Id, 1, null)));
    }

    [Fact]
    public async Task No_permite_reservar_en_empresa_no_verificada()
    {
        var a = _bd.CrearActividad(_bd.CrearEmpresa(EstadoEmpresa.Suspendida));
        var t = _bd.CrearTurista();

        await Assert.ThrowsAsync<NoEncontradoException>(() => Servicio(t.Id).CrearAsync(new CrearReservaRequest(a.Id, 1, null)));
    }

    [Fact]
    public async Task Concurrencia_optimista_detecta_cambios_simultaneos_en_cupos()
    {
        var a = _bd.CrearActividad(_bd.CrearEmpresa(), capacidad: 4);

        // Dos "peticiones" leen la misma versión de la actividad...
        await using var ctx1 = _bd.NuevoContexto();
        await using var ctx2 = _bd.NuevoContexto();
        var a1 = await ctx1.Actividades.SingleAsync(x => x.Id == a.Id);
        var a2 = await ctx2.Actividades.SingleAsync(x => x.Id == a.Id);
        a1.ReservarCupos(3, _bd.Reloj.Hoy);
        a2.ReservarCupos(3, _bd.Reloj.Hoy);

        // ...la primera gana y la segunda es rechazada en lugar de sobrevender.
        await ctx1.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctx2.SaveChangesAsync());
        Assert.Equal(3, (await _bd.NuevoContexto().Actividades.FindAsync(a.Id))!.CuposReservados);
    }

    [Fact]
    public async Task Resena_solo_tras_completar_y_una_vez()
    {
        var empresa = _bd.CrearEmpresa();
        var a = _bd.CrearActividad(empresa, diasHastaInicio: 10);
        var t = _bd.CrearTurista();
        var r = await Servicio(t.Id).CrearAsync(new CrearReservaRequest(a.Id, 1, null));

        // Aún no se realiza: no puede reseñar
        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            Servicio(t.Id).CrearResenaAsync(a.Id, new CrearResenaRequest(5, "Excelente experiencia")));

        // Se confirma y pasa la fecha
        var reserva = await _bd.Db.Reservas.Include(x => x.Actividad).SingleAsync(x => x.Id == r.Id);
        reserva.Confirmar();
        await _bd.Db.SaveChangesAsync();
        _bd.Reloj.Hoy = _bd.Reloj.Hoy.AddDays(20);

        await Servicio(t.Id).CrearResenaAsync(a.Id, new CrearResenaRequest(5, "Excelente experiencia"));
        await Assert.ThrowsAsync<ConflictoException>(() =>
            Servicio(t.Id).CrearResenaAsync(a.Id, new CrearResenaRequest(4, "Otra opinión más")));
    }

    [Fact]
    public async Task Cancelacion_por_la_empresa_cancela_las_reservas_activas()
    {
        var empresa = _bd.CrearEmpresa();
        var a = _bd.CrearActividad(empresa);
        var t = _bd.CrearTurista();
        await Servicio(t.Id).CrearAsync(new CrearReservaRequest(a.Id, 2, null));

        var servicioEmpresa = new ActividadesEmpresaService(_bd.Db, _bd.Reloj,
            new UsuarioActualFijo(empresa.UsuarioId, Rol.Empresa), new GuardarActividadValidador(_bd.Reloj));
        await servicioEmpresa.CancelarAsync(a.Id, "Lluvias intensas en la ruta");

        await using var ctx = _bd.NuevoContexto();
        var reserva = await ctx.Reservas.SingleAsync();
        Assert.Equal(EstadoReserva.Cancelada, reserva.Estado);
        Assert.Contains("Lluvias", reserva.MotivoEstado);
        Assert.Equal(0, (await ctx.Actividades.FindAsync(a.Id))!.CuposReservados);
    }

    public void Dispose() => _bd.Dispose();
}

public sealed class CatalogoYEmpresaTests : IDisposable
{
    private readonly BaseDatosPrueba _bd = new();

    [Fact]
    public async Task Catalogo_muestra_solo_publicadas_futuras_de_empresas_verificadas()
    {
        var verificada = _bd.CrearEmpresa();
        var pendiente = _bd.CrearEmpresa(EstadoEmpresa.Pendiente, "20100000002");
        var visible = _bd.CrearActividad(verificada);
        _bd.CrearActividad(verificada, estado: EstadoActividad.Borrador);
        _bd.CrearActividad(verificada, diasHastaInicio: -5);
        _bd.CrearActividad(pendiente);

        var servicio = new CatalogoService(_bd.Db, _bd.Reloj, new UsuarioActualFijo(null, null));
        var r = await servicio.BuscarAsync(new FiltroCatalogo());

        Assert.Equal(visible.Id, Assert.Single(r.Items).Id);
    }

    [Fact]
    public async Task Catalogo_valida_filtros_incoherentes()
    {
        var servicio = new CatalogoService(_bd.Db, _bd.Reloj, new UsuarioActualFijo(null, null));
        var ex = await Assert.ThrowsAsync<ValidacionException>(() =>
            servicio.BuscarAsync(new FiltroCatalogo(PrecioMin: 500, PrecioMax: 100, Orden: "aleatorio")));
        Assert.Contains("precioMax", ex.Errores.Keys);
        Assert.Contains("orden", ex.Errores.Keys);
    }

    [Fact]
    public async Task Empresa_no_ve_ni_edita_actividades_ajenas()
    {
        var mia = _bd.CrearEmpresa();
        var otra = _bd.CrearEmpresa(ruc: "20100000003");
        var ajena = _bd.CrearActividad(otra);
        var servicio = new ActividadesEmpresaService(_bd.Db, _bd.Reloj,
            new UsuarioActualFijo(mia.UsuarioId, Rol.Empresa), new GuardarActividadValidador(_bd.Reloj));

        await Assert.ThrowsAsync<NoEncontradoException>(() => servicio.ObtenerAsync(ajena.Id));
        await Assert.ThrowsAsync<NoEncontradoException>(() => servicio.CancelarAsync(ajena.Id, "Motivo válido"));
    }

    [Fact]
    public async Task Empresa_pendiente_no_puede_publicar()
    {
        var pendiente = _bd.CrearEmpresa(EstadoEmpresa.Pendiente);
        var borrador = _bd.CrearActividad(pendiente, estado: EstadoActividad.Borrador);
        var servicio = new ActividadesEmpresaService(_bd.Db, _bd.Reloj,
            new UsuarioActualFijo(pendiente.UsuarioId, Rol.Empresa), new GuardarActividadValidador(_bd.Reloj));

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.PublicarAsync(borrador.Id));
        Assert.Contains("verificada", ex.Message);
    }

    [Fact]
    public async Task Validador_de_actividad_rechaza_fechas_pasadas_y_precio_negativo()
    {
        var v = new GuardarActividadValidador(_bd.Reloj);
        var hoy = _bd.Reloj.Hoy;
        var r = await v.ValidateAsync(new GuardarActividadRequest("Tour", "corta", "", "", hoy.AddDays(-1), hoy.AddDays(-3), -5, 0, null, null, "ftp://x", null));
        var campos = r.Errors.Select(e => e.PropertyName).ToHashSet();

        Assert.Superset(new HashSet<string> { "Nombre", "Descripcion", "Destino", "PuntoEncuentro", "FechaInicio", "FechaFin", "Precio", "Capacidad", "ImagenUrl" }, campos);
    }

    [Theory]
    [InlineData("=HYPERLINK(\"x\")", "\"'=HYPERLINK(\"\"x\"\")\"")]
    [InlineData("+51 999", "'+51 999")]
    [InlineData("Pérez; Ana", "\"Pérez; Ana\"")]
    [InlineData("Normal", "Normal")]
    public void Celda_csv_neutraliza_formulas_y_escapa_separadores(string entrada, string esperado)
    {
        Assert.Equal(esperado, ActividadesEmpresaService.Celda(entrada));
    }

    [Theory]
    [InlineData("Ana", "Quispe Mamani", "Ana Q.")]
    [InlineData("María José", "torres", "María T.")]
    public void Nombre_publico_de_resenas_se_abrevia(string nombre, string apellidos, string esperado)
    {
        Assert.Equal(esperado, CatalogoService.NombrePublico(nombre, apellidos));
    }

    public void Dispose() => _bd.Dispose();
}
