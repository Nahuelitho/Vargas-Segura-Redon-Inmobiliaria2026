using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Models;

public class Pago
{
    public int Id { get; set; }
    public int IdReserva { get; set; }
    public string Concepto { get; set; } = "";
    public DateTime FechaPago { get; set; }
    public decimal Importe { get; set; }
    public bool Estado { get; set; } = true;
    public bool EsSena { get; set; }
    public int? IdUsuarioCreador { get; set; }
    public int? IdUsuarioAnulador { get; set; }
    public string? UsuarioCreador { get; set; }
    public string? UsuarioAnulador { get; set; }
}

public class PagoConceptoFormulario
{
    [Required(ErrorMessage = "Ingrese el concepto.")]
    [StringLength(255, ErrorMessage = "El concepto admite hasta 255 caracteres.")]
    public string Concepto { get; set; } = "";
}

public class PagoFormulario : PagoConceptoFormulario, IValidatableObject
{
    [Required(ErrorMessage = "Ingrese la fecha del pago.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha del pago")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(Inmobiliaria.Services.FechaReservaBinder))]
    public DateTime? FechaPago { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Ingrese el importe.")]
    [Range(typeof(decimal), "0.01", "99999999.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "El importe debe ser mayor que cero y no superar 99.999.999,99.")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(Inmobiliaria.Services.ImportePagoBinder))]
    public decimal? Importe { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Importe.HasValue && decimal.Round(Importe.Value, 2) != Importe.Value)
            yield return new ValidationResult("El importe admite hasta dos decimales.", new[] { nameof(Importe) });
        if (FechaPago.HasValue && FechaPago.Value.Year < 1000)
            yield return new ValidationResult("Ingrese una fecha válida.", new[] { nameof(FechaPago) });
    }
}

public class PagosReservaVista
{
    public Reserva Reserva { get; set; } = new();
    public List<Pago> Pagos { get; set; } = new();
    public decimal? SenaEsperada { get; set; }
    public decimal TotalPagado => Pagos.Where(p => p.Estado).Sum(p => p.Importe);
}
