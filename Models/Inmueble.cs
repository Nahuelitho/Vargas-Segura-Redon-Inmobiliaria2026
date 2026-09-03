using System.ComponentModel.DataAnnotations;
namespace Inmobiliaria.Models;

public class Inmueble
{
    public int Id { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un propietario.")]
    public int IdPropietario { get; set; }
    public Propietario? Propietario { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un tipo de inmueble.")]
    public int IdTipo { get; set; }
    public TipoInmueble? Tipo { get; set; }

    [Required]
    public string Direccion { get; set; } = "";
    [Range(1, int.MaxValue)]
    public int Cupo { get; set; }
    public string? Coordenadas { get; set; }
    [Range(0, double.MaxValue)]
    public decimal PrecioPorDia { get; set; }
    [Range(0, 100)]
    public decimal PorcentajeReserva { get; set; }
    public string? ImagenPortada { get; set; }
    public bool Disponible { get; set; } = true;
    public bool Estado { get; set; } = true;
}