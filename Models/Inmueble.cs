using System.ComponentModel.DataAnnotations;
namespace Inmobiliaria.Models;

public class Inmueble
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Seleccione un propietario")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un propietario.")]
    public int IdPropietario { get; set; }
    public Propietario? Propietario { get; set; }

    [Required(ErrorMessage = "Seleccione un tipo de inmueble")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un tipo de inmueble.")]
    public int IdTipo { get; set; }
    public TipoInmueble? Tipo { get; set; }

    [Required(ErrorMessage = "La direccion es obligatoria")]
    public string Direccion { get; set; } = "";

    [Required(ErrorMessage = "Ingrese el cupo")]
    [Range(1, 10, ErrorMessage = "Seleccione un numero entre 1 y 10.")]
    public int Cupo { get; set; }

    [Required(ErrorMessage = "Ingrese las Coordenadas")]    
    public string? Coordenadas { get; set; }

    

    [Required(ErrorMessage = "Ingrese el precio por dia")]
    [Range(0, double.MaxValue, ErrorMessage = "Ingrese un precio valido")]
    public decimal? PrecioPorDia { get; set; }

    
    [Required(ErrorMessage = "Ingrese el porcentaje de reserva")]
    [Range(0, 100, ErrorMessage ="Ingrese un porcentaje entre 0 y 100")]
    public decimal? PorcentajeReserva { get; set; }
    public string? ImagenPortada { get; set; }

    // Archivo subido por el usuario para la imagen de portada.
    // No se persiste en BD; su contenido se guarda en disco y el nombre en ImagenPortada.
    public IFormFile? ArchivoPortada { get; set; }

    public bool Disponible { get; set; } = true;
    public bool Estado { get; set; } = true;
}
