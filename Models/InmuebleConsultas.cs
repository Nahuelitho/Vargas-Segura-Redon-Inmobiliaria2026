using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Models;

public class BusquedaInmueblesFiltro : IValidatableObject
{
    [Required(ErrorMessage = "Ingrese la fecha de inicio.")]
    [DataType(DataType.Date)]
    public DateTime? FechaInicio { get; set; }

    [Required(ErrorMessage = "Ingrese la fecha de fin.")]
    [DataType(DataType.Date)]
    public DateTime? FechaFin { get; set; }

    public int? IdTipo { get; set; }
    [Range(1, 10, ErrorMessage = "El cupo debe estar entre 1 y 10.")]
    public int? CupoMinimo { get; set; }
    [Range(0, double.MaxValue, ErrorMessage = "Ingrese un precio válido.")]
    public decimal? PrecioMaximo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaInicio.HasValue && FechaFin.HasValue && FechaFin < FechaInicio)
            yield return new ValidationResult("La fecha de fin no puede ser anterior a la fecha de inicio.", new[] { nameof(FechaFin) });
    }
}

public class ListadoInmueblesVista
{
    public List<Inmueble> Inmuebles { get; set; } = new();
    public int Pagina { get; set; }
    public int TotalPaginas { get; set; }
    public int Total { get; set; }
}

public class InmuebleReservado : Inmueble
{
    public int CantidadReservas { get; set; }
}
