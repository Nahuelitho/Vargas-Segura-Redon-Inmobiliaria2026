using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Models;

public class ImagenInmueble
{
    public int Id { get; set; }
    public int IdInmueble { get; set; }

    // Almacena el nombre del archivo en disco (ej: "a1b2c3...webp").
    // No lo completa el usuario directamente — se asigna tras guardar el archivo subido.
    public string Url { get; set; } = "";

    // Archivo subido por el usuario a través del formulario.
    [Required(ErrorMessage = "Seleccione una imagen.")]
    public IFormFile? Archivo { get; set; }

    public bool Estado { get; set; } = true;
}

public class GaleriaInmuebleVista
{
    public Inmueble Inmueble { get; set; } = new();
    public List<ImagenInmueble> Imagenes { get; set; } = new();
    public ImagenInmueble NuevaImagen { get; set; } = new();
}
