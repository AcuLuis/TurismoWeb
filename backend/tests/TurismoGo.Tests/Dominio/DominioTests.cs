using TurismoGo.Domain;
using TurismoGo.Domain.Entidades;

namespace TurismoGo.Tests.Dominio;

public class ActividadTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 14);

    private static Actividad Nueva(int capacidad = 10, int dias = 5, EstadoActividad estado = EstadoActividad.Publicada) => new()
    {
        Capacidad = capacidad,
        FechaInicio = Hoy.AddDays(dias),
        FechaFin = Hoy.AddDays(dias + 2),
        Estado = estado,
        Itinerario = [new ItinerarioDia { Dia = 1, Titulo = "t", Descripcion = "d" }],
    };

    [Fact]
    public void ReservarCupos_descuenta_y_cambia_la_version()
    {
        var a = Nueva();
        var version = a.Version;

        a.ReservarCupos(4, Hoy);

        Assert.Equal(4, a.CuposReservados);
        Assert.Equal(6, a.CuposDisponibles);
        Assert.NotEqual(version, a.Version);
    }

    [Fact]
    public void ReservarCupos_impide_sobreventa()
    {
        var a = Nueva(capacidad: 3);
        var ex = Assert.Throws<ReglaNegocioException>(() => a.ReservarCupos(4, Hoy));
        Assert.Contains("3 cupos", ex.Message);
    }

    [Theory]
    [InlineData(0)]   // empieza hoy
    [InlineData(-1)]  // ya empezó
    public void ReservarCupos_rechaza_actividades_que_ya_comenzaron(int dias)
    {
        var a = Nueva(dias: dias);
        Assert.Throws<ReglaNegocioException>(() => a.ReservarCupos(1, Hoy));
    }

    [Fact]
    public void ReservarCupos_rechaza_borradores()
    {
        var a = Nueva(estado: EstadoActividad.Borrador);
        Assert.Throws<ReglaNegocioException>(() => a.ReservarCupos(1, Hoy));
    }

    [Fact]
    public void LiberarCupos_no_baja_de_cero()
    {
        var a = Nueva();
        a.ReservarCupos(2, Hoy);
        a.LiberarCupos(5);
        Assert.Equal(0, a.CuposReservados);
    }

    [Fact]
    public void Publicar_exige_itinerario_y_fecha_futura()
    {
        var sinItinerario = Nueva(estado: EstadoActividad.Borrador);
        sinItinerario.Itinerario.Clear();
        Assert.Throws<ReglaNegocioException>(() => sinItinerario.Publicar(Hoy));

        var pasada = Nueva(dias: 0, estado: EstadoActividad.Borrador);
        Assert.Throws<ReglaNegocioException>(() => pasada.Publicar(Hoy));

        var valida = Nueva(estado: EstadoActividad.Borrador);
        valida.Publicar(Hoy);
        Assert.Equal(EstadoActividad.Publicada, valida.Estado);
    }

    [Fact]
    public void DuracionDias_incluye_el_primer_y_ultimo_dia()
    {
        Assert.Equal(3, Nueva().DuracionDias);
    }
}

public class ReservaTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 14);
    private static readonly DateTime Ahora = new(2026, 11, 14, 15, 0, 0, DateTimeKind.Utc);

    private static Reserva Nueva(int diasHastaInicio, EstadoReserva estado = EstadoReserva.Confirmada)
    {
        var actividad = new Actividad
        {
            Capacidad = 10, CuposReservados = 2, Estado = EstadoActividad.Publicada,
            FechaInicio = Hoy.AddDays(diasHastaInicio), FechaFin = Hoy.AddDays(diasHastaInicio),
        };
        return new Reserva { Actividad = actividad, CantidadPersonas = 2, Estado = estado };
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(5, true)]
    [InlineData(1, false)]
    [InlineData(0, false)]
    public void Politica_de_cancelacion_de_dos_dias(int diasHastaInicio, bool permitido)
    {
        Assert.Equal(permitido, Nueva(diasHastaInicio).PuedeCancelarTurista(Hoy, 2));
    }

    [Fact]
    public void Cancelar_libera_cupos_y_registra_motivo()
    {
        var r = Nueva(10);
        r.CancelarPorTurista(Hoy, 2, Ahora, "Cambio de planes");

        Assert.Equal(EstadoReserva.Cancelada, r.Estado);
        Assert.Equal(0, r.Actividad.CuposReservados);
        Assert.Equal("Cambio de planes", r.MotivoEstado);
        Assert.Equal(Ahora, r.CanceladaEn);
    }

    [Fact]
    public void Cancelar_fuera_de_plazo_falla()
    {
        var ex = Assert.Throws<ReglaNegocioException>(() => Nueva(1).CancelarPorTurista(Hoy, 2, Ahora, null));
        Assert.Contains("2 días", ex.Message);
    }

    [Fact]
    public void Solo_se_confirman_o_rechazan_pendientes()
    {
        var pendiente = Nueva(10, EstadoReserva.Pendiente);
        pendiente.Confirmar();
        Assert.Equal(EstadoReserva.Confirmada, pendiente.Estado);
        Assert.Throws<ReglaNegocioException>(() => pendiente.Confirmar());
        Assert.Throws<ReglaNegocioException>(() => pendiente.Rechazar("motivo"));
    }

    [Fact]
    public void Rechazar_libera_cupos()
    {
        var r = Nueva(10, EstadoReserva.Pendiente);
        r.Rechazar("Sin guía disponible");
        Assert.Equal(EstadoReserva.Rechazada, r.Estado);
        Assert.Equal(0, r.Actividad.CuposReservados);
    }

    [Fact]
    public void Completada_es_confirmada_con_actividad_finalizada()
    {
        Assert.True(Nueva(-3).EstaCompletada(Hoy));
        Assert.False(Nueva(3).EstaCompletada(Hoy));
        Assert.False(Nueva(-3, EstadoReserva.Pendiente).EstaCompletada(Hoy));
    }

    [Fact]
    public void CodigoReserva_tiene_formato_legible_y_no_se_repite()
    {
        var codigos = Enumerable.Range(0, 500).Select(_ => CodigoReserva.Generar()).ToList();
        Assert.All(codigos, c => Assert.Matches("^TG-[2-9A-HJKMNP-Z]{8}$", c));
        Assert.Equal(codigos.Count, codigos.Distinct().Count());
    }
}
