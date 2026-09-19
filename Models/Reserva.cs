using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Models;

public class Reserva
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Seleccione un inquilino")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un inquilino.")]
    public int IdInquilino { get; set; }

    public Inquilino? Inquilino { get; set; }

    [Required(ErrorMessage = "Seleccione un inmueble")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un inmueble.")]
    public int IdInmueble { get; set; }

    public Inmueble? Inmueble { get; set; }

    [DataType(DataType.Date)]
    [Microsoft.AspNetCore.Mvc.ModelBinder(
        BinderType = typeof(Inmobiliaria.Services.FechaReservaBinder)
    )]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Microsoft.AspNetCore.Mvc.ModelBinder(
        BinderType = typeof(Inmobiliaria.Services.FechaReservaBinder)
    )]
    public DateTime FechaFin { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "Ingrese el monto por dia")]
    [Range(0, double.MaxValue, ErrorMessage = "Ingrese un monto valido")]
    public decimal? MontoPorDia { get; set; }

    [DataType(DataType.Date)]
    [Microsoft.AspNetCore.Mvc.ModelBinder(
        BinderType = typeof(Inmobiliaria.Services.FechaReservaBinder)
    )]
    public DateTime? FechaTerminacion { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Ingrese una multa valida")]
    public decimal? Multa { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Seleccione una reserva de origen válida o deje el campo sin origen."
    )]
    public int? IdReservaOrigen { get; set; }

    public int? IdUsuarioCreador { get; set; }

    public int? IdUsuarioTerminador { get; set; }

    public string? UsuarioCreador { get; set; }

    public string? UsuarioTerminador { get; set; }

    public bool Estado { get; set; } = true;
}