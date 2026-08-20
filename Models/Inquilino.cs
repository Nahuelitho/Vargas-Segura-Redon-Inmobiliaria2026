namespace Inmobiliaria.Models;

public class Inquilino
{
    public int Id { get; set; }
    public string Dni { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public bool Estado { get; set; } = true;
}