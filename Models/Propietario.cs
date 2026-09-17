using System.ComponentModel.DataAnnotations;
namespace Inmobiliaria.Models;

public class Propietario
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ingrese el DNI")]
    public string Dni { get; set; } = "";

    [Required(ErrorMessage = "Ingrese el nombre")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo puede contener letras")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingrese el apellido")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El apellido solo puede contener letras")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "Ingrese el telefono")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "Ingrese el email")]
    [EmailAddress(ErrorMessage = "Ingrese un email valido")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ingrese la direccion")]
    public string Direccion { get; set; } = "";

    public bool Estado { get; set; }
}
