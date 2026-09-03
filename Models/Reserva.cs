using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Models;

public class Reserva
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un inquilino.")]
    public int IdInquilino { get; set; }
    public Inquilino? Inquilino { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un inmueble.")]
    public int IdInmueble { get; set; }
    public Inmueble? Inmueble { get; set; }

    [DataType(DataType.Date)]
    public DateTime FechaInicio { get; set; } = DateTime.Today;
    [DataType(DataType.Date)]
    public DateTime FechaFin { get; set; } = DateTime.Today.AddDays(1);
    [Range(0, double.MaxValue)]
    public decimal MontoPorDia { get; set; }
    [DataType(DataType.Date)]
    public DateTime? FechaTerminacion { get; set; }
    [Range(0, double.MaxValue)]
    public decimal? Multa { get; set; }
    public int? IdReservaOrigen { get; set; }
    public bool Estado { get; set; } = true;
}
