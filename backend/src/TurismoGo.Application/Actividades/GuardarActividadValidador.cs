using FluentValidation;
using TurismoGo.Application.Abstracciones;
using TurismoGo.Application.Comun;

namespace TurismoGo.Application.Actividades;

public sealed class GuardarActividadValidador : AbstractValidator<GuardarActividadRequest>
{
    public const int DuracionMaximaDias = 30;

    public GuardarActividadValidador(IReloj reloj)
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("Ingresa el nombre de la actividad.")
            .MinimumLength(5).WithMessage("El nombre debe tener al menos 5 caracteres.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");
        RuleFor(x => x.Descripcion).NotEmpty().WithMessage("Ingresa una descripción.")
            .MinimumLength(20).WithMessage("La descripción debe tener al menos 20 caracteres.")
            .MaximumLength(2000).WithMessage("La descripción no puede superar los 2000 caracteres.");
        RuleFor(x => x.Destino).NotEmpty().WithMessage("Ingresa el destino.")
            .MaximumLength(80).WithMessage("El destino no puede superar los 80 caracteres.");
        RuleFor(x => x.PuntoEncuentro).NotEmpty().WithMessage("Ingresa el punto de encuentro.")
            .MaximumLength(200).WithMessage("El punto de encuentro no puede superar los 200 caracteres.");

        RuleFor(x => x.FechaInicio).Must(f => f > reloj.Hoy)
            .WithMessage("La fecha de inicio debe ser posterior a hoy.");
        RuleFor(x => x.FechaFin).GreaterThanOrEqualTo(x => x.FechaInicio)
            .WithMessage("La fecha de fin debe ser igual o posterior a la de inicio.");
        RuleFor(x => x).Must(x => x.FechaFin.DayNumber - x.FechaInicio.DayNumber + 1 <= DuracionMaximaDias)
            .When(x => x.FechaFin >= x.FechaInicio)
            .WithName("fechaFin").OverridePropertyName("fechaFin")
            .WithMessage($"La actividad no puede durar más de {DuracionMaximaDias} días.");

        RuleFor(x => x.Precio).GreaterThan(0).WithMessage("El precio debe ser mayor que 0.")
            .LessThanOrEqualTo(50000).WithMessage("El precio no puede superar S/ 50 000.")
            .PrecisionScale(10, 2, true).WithMessage("El precio admite como máximo 2 decimales.");
        RuleFor(x => x.Capacidad).InclusiveBetween(1, 500).WithMessage("La capacidad debe estar entre 1 y 500 personas.");

        RuleFor(x => x.Incluye).MaximumLength(1000).WithMessage("Máximo 1000 caracteres.");
        RuleFor(x => x.NoIncluye).MaximumLength(1000).WithMessage("Máximo 1000 caracteres.");
        RuleFor(x => x.ImagenUrl).UrlHttp();

        RuleFor(x => x.Itinerario).Must(i => i is null || i.Count <= DuracionMaximaDias)
            .WithMessage($"El itinerario admite como máximo {DuracionMaximaDias} días.");
        RuleFor(x => x.Itinerario).Must(i => i is null || i.Select(d => d.Dia).Distinct().Count() == i.Count)
            .WithMessage("Cada día del itinerario debe ser único.");
        RuleForEach(x => x.Itinerario).ChildRules(dia =>
        {
            dia.RuleFor(d => d.Titulo).NotEmpty().WithMessage("Ingresa un título para el día.")
                .MaximumLength(100).WithMessage("El título no puede superar los 100 caracteres.");
            dia.RuleFor(d => d.Descripcion).NotEmpty().WithMessage("Describe las actividades del día.")
                .MaximumLength(1000).WithMessage("La descripción no puede superar los 1000 caracteres.");
        });
        RuleForEach(x => x.Itinerario)
            .Must((x, d) => d.Dia >= 1 && d.Dia <= Math.Max(1, x.FechaFin.DayNumber - x.FechaInicio.DayNumber + 1))
            .WithMessage("El día del itinerario debe estar dentro de la duración de la actividad.");
    }
}
