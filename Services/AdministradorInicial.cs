using System.ComponentModel.DataAnnotations;
using System.Text;
using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Inmobiliaria.Services;

public static class AdministradorInicial
{
    public static async Task Ejecutar(IServiceProvider servicios)
    {
        using var scope = servicios.CreateScope();
        var repositorio = (UsuarioRepository)scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        await repositorio.InicializarTabla();
        Console.WriteLine("Crear el primer administrador (no modifica usuarios existentes).");
        Console.Write("Nombre: "); var nombre = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Apellido: "); var apellido = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Email: "); var email = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Contraseña (10 a 128 caracteres): "); var password = LeerPassword();
        Console.Write("Repetir contraseña: "); var confirmacion = LeerPassword();
        var formulario = new UsuarioFormulario { Nombre = nombre, Apellido = apellido, Email = email, Rol = Roles.Administrador, Password = password, Confirmacion = confirmacion };
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(formulario, new ValidationContext(formulario), errores, true) || string.IsNullOrEmpty(password))
            throw new InvalidOperationException("Datos inválidos. Revise nombre, apellido, email y las contraseñas (10 a 128 caracteres, iguales).");
        var usuario = new Usuario { Nombre = nombre, Apellido = apellido, Email = email, Rol = Roles.Administrador };
        usuario.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>().HashPassword(usuario, password);
        await repositorio.Crear(usuario, primerAdministrador: true);
        Console.WriteLine("Administrador creado. Inicie la aplicación y acceda con su email y contraseña.");
    }

    private static string LeerPassword()
    {
        if (Console.IsInputRedirected) return Console.ReadLine() ?? "";
        var texto = new StringBuilder();
        ConsoleKeyInfo tecla;
        while ((tecla = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
        {
            if (tecla.Key == ConsoleKey.Backspace && texto.Length > 0) texto.Length--;
            else if (!char.IsControl(tecla.KeyChar)) texto.Append(tecla.KeyChar);
        }
        Console.WriteLine();
        return texto.ToString();
    }
}
