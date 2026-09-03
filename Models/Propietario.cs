using System.ComponentModel.DataAnnotations;
namespace Inmobiliaria.Models;

public class Propietario
{
    public int Id { get; set; }

    [Required]
    public string Dni { get; set; } = "";

    [Required]
    public string Nombre { get; set; } = "";

    [Required]
    public string Apellido { get; set; } = "";

    [Required]
    public string Telefono { get; set; } = "";

    [Required]
    public string Email { get; set; } = "";

    [Required]
    public string Direccion { get; set; } = "";

    public bool Estado { get; set; }
}