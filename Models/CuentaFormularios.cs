using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Models;

public class LoginFormulario
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";
    [Required, DataType(DataType.Password), Display(Name = "Contraseña")]
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public class PerfilFormulario
{
    [Required, StringLength(100)]
    public string Nombre { get; set; } = "";
    [Required, StringLength(100)]
    public string Apellido { get; set; } = "";
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";
    public IFormFile? ArchivoAvatar { get; set; }
    public bool QuitarAvatar { get; set; }
    public bool TieneAvatar { get; set; }
}

public class PasswordFormulario
{
    [Required, DataType(DataType.Password), Display(Name = "Contraseña actual")]
    public string PasswordActual { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 10), DataType(DataType.Password), Display(Name = "Nueva contraseña")]
    public string PasswordNueva { get; set; } = "";
    [Required, Compare(nameof(PasswordNueva), ErrorMessage = "Las contraseñas no coinciden."), DataType(DataType.Password), Display(Name = "Repetir nueva contraseña")]
    public string Confirmacion { get; set; } = "";
}

public class UsuarioFormulario
{
    public int Id { get; set; }
    [Required, StringLength(100)]
    public string Nombre { get; set; } = "";
    [Required, StringLength(100)]
    public string Apellido { get; set; } = "";
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";
    [Required, RegularExpression("^(Administrador|Empleado)$", ErrorMessage = "Seleccione un rol válido.")]
    public string Rol { get; set; } = Roles.Empleado;
    [StringLength(128, MinimumLength = 10), DataType(DataType.Password), Display(Name = "Contraseña")]
    public string? Password { get; set; }
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden."), DataType(DataType.Password), Display(Name = "Repetir contraseña")]
    public string? Confirmacion { get; set; }
}

public record UsuariosListado(IReadOnlyList<Usuario> Usuarios, int Pagina, int TotalPaginas, string? Buscar);
