using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Inmobiliaria.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Inmobiliaria.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CuentaController(IUsuarioRepository usuarios, IPasswordHasher<Usuario> hasher, AvatarService avatares) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Home") : View(new LoginFormulario { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginFormulario formulario)
    {
        if (!ModelState.IsValid) return View(formulario);
        var usuario = await usuarios.ObtenerPorEmail(formulario.Email);
        if (usuario is null || !usuario.Estado || hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, formulario.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            return View(formulario);
        }
        await Seguridad.IniciarSesion(HttpContext, usuario);
        if (Url.IsLocalUrl(formulario.ReturnUrl)) return LocalRedirect(formulario.ReturnUrl!);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous, HttpGet]
    public IActionResult AccesoDenegado()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        var usuario = await usuarios.ObtenerPorId(Seguridad.IdUsuario(User));
        if (usuario is null) return Challenge();
        return View(new PerfilFormulario { Nombre = usuario.Nombre, Apellido = usuario.Apellido, Email = usuario.Email, TieneAvatar = usuario.Avatar is not null });
    }

    [HttpPost, RequestSizeLimit(3 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 3 * 1024 * 1024)]
    public async Task<IActionResult> Perfil(PerfilFormulario formulario)
    {
        var id = Seguridad.IdUsuario(User); // Nunca se toma el usuario ni su rol del formulario.
        var usuario = await usuarios.ObtenerPorId(id);
        if (usuario is null) return Challenge();
        formulario.TieneAvatar = usuario.Avatar is not null;
        if (!ModelState.IsValid) return View(formulario);
        string? nuevoAvatar = null;
        try
        {
            if (formulario.ArchivoAvatar is not null) nuevoAvatar = await avatares.Guardar(formulario.ArchivoAvatar);
            var avatar = nuevoAvatar ?? (formulario.QuitarAvatar ? null : usuario.Avatar);
            await usuarios.ActualizarPerfil(id, formulario, avatar);
        }
        catch (InvalidOperationException ex)
        {
            avatares.Eliminar(nuevoAvatar);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(formulario);
        }
        var actualizado = await usuarios.ObtenerPorId(id);
        if (actualizado is null) return Challenge();
        if (usuario.Avatar != actualizado.Avatar) avatares.Eliminar(usuario.Avatar);
        await Seguridad.IniciarSesion(HttpContext, actualizado);
        TempData["Mensaje"] = "Perfil actualizado.";
        return RedirectToAction(nameof(Perfil));
    }

    [HttpGet]
    public IActionResult Password() => View(new PasswordFormulario());

    [HttpPost]
    public async Task<IActionResult> Password(PasswordFormulario formulario)
    {
        if (!ModelState.IsValid) return View(formulario);
        var usuario = await usuarios.ObtenerPorId(Seguridad.IdUsuario(User));
        if (usuario is null) return Challenge();
        if (hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, formulario.PasswordActual) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(formulario.PasswordActual), "La contraseña actual es incorrecta.");
            return View(formulario);
        }
        try
        {
            await usuarios.CambiarPassword(usuario.Id, hasher.HashPassword(usuario, formulario.PasswordNueva), usuario.SelloSeguridad);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(formulario);
        }
        await HttpContext.SignOutAsync();
        TempData["Mensaje"] = "Contraseña actualizada. Inicie sesión con la nueva contraseña.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> Avatar()
    {
        var usuario = await usuarios.ObtenerPorId(Seguridad.IdUsuario(User));
        var ruta = avatares.Ruta(usuario?.Avatar);
        if (ruta is null || !System.IO.File.Exists(ruta)) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        var tipo = Path.GetExtension(ruta) switch { ".png" => "image/png", ".jpg" => "image/jpeg", _ => "image/webp" };
        return PhysicalFile(ruta, tipo);
    }
}
