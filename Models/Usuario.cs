namespace Inmobiliaria.Models;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Empleado = "Empleado";
}

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Rol { get; set; } = Roles.Empleado;
    public string? Avatar { get; set; }
    public string SelloSeguridad { get; set; } = Guid.NewGuid().ToString("N");
    public bool Estado { get; set; } = true;
}
