using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Inmobiliaria.Tests;

// Solo se usa en pruebas: ninguna prueba HTTP modifica la base de la inmobiliaria.
public class UsuariosMemoria : IUsuarioRepository
{
    public const string Clave = "Prueba.Segura.2026";
    public List<Usuario> Datos { get; } = new();
    public UsuariosMemoria()
    {
        var hasher = new PasswordHasher<Usuario>();
        foreach (var rol in new[] { Roles.Administrador, Roles.Empleado })
        {
            var usuario = new Usuario { Id = Datos.Count + 1, Nombre = rol, Apellido = "Prueba", Email = rol.ToLowerInvariant() + "@example.test", Rol = rol };
            usuario.PasswordHash = hasher.HashPassword(usuario, Clave);
            Datos.Add(usuario);
        }
    }
    private static Usuario? Copiar(Usuario? u) => u is null ? null : new Usuario { Id = u.Id, Nombre = u.Nombre, Apellido = u.Apellido, Email = u.Email, Rol = u.Rol, Avatar = u.Avatar, Estado = u.Estado, PasswordHash = u.PasswordHash, SelloSeguridad = u.SelloSeguridad };
    public Task<Usuario?> ObtenerPorId(int id) => Task.FromResult(Copiar(Datos.Find(u => u.Id == id && u.Estado)));
    public Task<Usuario?> ObtenerPorEmail(string email) => Task.FromResult(Copiar(Datos.Find(u => u.Email == UsuarioRepository.NormalizarEmail(email) && u.Estado)));
    public Task<UsuariosListado> Listar(int pagina, string? buscar) => Task.FromResult(new UsuariosListado(Datos.Where(u => u.Estado).Select(u => Copiar(u)!).ToList(), 1, 1, buscar));
    public Task<int> Crear(Usuario usuario, bool primerAdministrador = false)
    {
        if (Datos.Any(u => u.Email == UsuarioRepository.NormalizarEmail(usuario.Email))) throw new InvalidOperationException("El email ya está registrado.");
        usuario.Id = Datos.Max(u => u.Id) + 1;
        usuario.Email = UsuarioRepository.NormalizarEmail(usuario.Email);
        Datos.Add(Copiar(usuario)!);
        return Task.FromResult(usuario.Id);
    }
    public Task Actualizar(Usuario usuario)
    {
        var i = Datos.FindIndex(u => u.Id == usuario.Id);
        usuario.SelloSeguridad = Guid.NewGuid().ToString("N");
        Datos[i] = Copiar(usuario)!;
        return Task.CompletedTask;
    }
    public Task ActualizarPerfil(int id, PerfilFormulario perfil, string? avatar)
    {
        if (Datos.Any(u => u.Id != id && u.Email == UsuarioRepository.NormalizarEmail(perfil.Email))) throw new InvalidOperationException("El email ya está registrado.");
        var usuario = Datos.Single(u => u.Id == id);
        usuario.Nombre = perfil.Nombre; usuario.Apellido = perfil.Apellido;
        usuario.Email = UsuarioRepository.NormalizarEmail(perfil.Email); usuario.Avatar = avatar;
        usuario.SelloSeguridad = Guid.NewGuid().ToString("N");
        return Task.CompletedTask;
    }
    public Task CambiarPassword(int id, string hash, string selloAnterior)
    {
        var usuario = Datos.Single(u => u.Id == id);
        if (usuario.SelloSeguridad != selloAnterior) throw new InvalidOperationException("El usuario cambió.");
        usuario.PasswordHash = hash; usuario.SelloSeguridad = Guid.NewGuid().ToString("N");
        return Task.CompletedTask;
    }
    public Task Eliminar(int id)
    {
        Datos.Single(u => u.Id == id).Estado = false;
        return Task.CompletedTask;
    }
}
