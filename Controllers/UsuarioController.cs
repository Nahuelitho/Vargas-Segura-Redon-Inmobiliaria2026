using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Inmobiliaria.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Inmobiliaria.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class UsuarioController(IUsuarioRepository usuarios, IPasswordHasher<Usuario> hasher) : Controller
{
    public async Task<IActionResult> Index(int pagina = 1, string? buscar = null) => View(await usuarios.Listar(pagina, buscar));

    [HttpGet]
    public IActionResult Crear() => View(new UsuarioFormulario());

    [HttpPost]
    public async Task<IActionResult> Crear(UsuarioFormulario formulario)
    {
        if (string.IsNullOrEmpty(formulario.Password)) ModelState.AddModelError(nameof(formulario.Password), "Ingrese una contraseña de al menos 10 caracteres.");
        if (!ModelState.IsValid) return View(formulario);
        var usuario = new Usuario { Nombre = formulario.Nombre, Apellido = formulario.Apellido, Email = formulario.Email, Rol = formulario.Rol };
        usuario.PasswordHash = hasher.HashPassword(usuario, formulario.Password!);
        try { await usuarios.Crear(usuario); }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(formulario); }
        TempData["Mensaje"] = "Usuario creado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        if (id == Seguridad.IdUsuario(User)) return RedirectToAction("Perfil", "Cuenta");
        var usuario = await usuarios.ObtenerPorId(id);
        if (usuario is null) return NotFound();
        return View(new UsuarioFormulario { Id = usuario.Id, Nombre = usuario.Nombre, Apellido = usuario.Apellido, Email = usuario.Email, Rol = usuario.Rol });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, UsuarioFormulario formulario)
    {
        if (id != formulario.Id) return BadRequest();
        if (id == Seguridad.IdUsuario(User)) return BadRequest("Edite sus propios datos desde Mi perfil.");
        if (!ModelState.IsValid) return View(formulario);
        var usuario = await usuarios.ObtenerPorId(id);
        if (usuario is null) return NotFound();
        usuario.Nombre = formulario.Nombre;
        usuario.Apellido = formulario.Apellido;
        usuario.Email = formulario.Email;
        usuario.Rol = formulario.Rol;
        if (!string.IsNullOrEmpty(formulario.Password)) usuario.PasswordHash = hasher.HashPassword(usuario, formulario.Password);
        try { await usuarios.Actualizar(usuario); }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(formulario); }
        TempData["Mensaje"] = "Usuario actualizado. Sus sesiones anteriores quedaron cerradas.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(int id)
    {
        if (id == Seguridad.IdUsuario(User)) return BadRequest("No puede dar de baja su propia cuenta.");
        try { await usuarios.Eliminar(id); TempData["Mensaje"] = "Usuario dado de baja."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
