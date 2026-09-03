using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Inmobiliaria.Controllers;

public class TipoInmuebleController(TipoInmuebleRepository repositorio) : Controller
{
    public async Task<IActionResult> Index() => View(await repositorio.ObtenerTodos());
    public IActionResult Crear() => View(new TipoInmueble());
    [HttpPost] public async Task<IActionResult> Crear(TipoInmueble tipo) => await Guardar(tipo, false);
    public async Task<IActionResult> Editar(int id) => await repositorio.ObtenerPorId(id) is { } tipo ? View(tipo) : NotFound();
    [HttpPost] public async Task<IActionResult> Editar(TipoInmueble tipo) => await Guardar(tipo, true);
    public async Task<IActionResult> Detalles(int id) => await repositorio.ObtenerPorId(id) is { } tipo ? View(tipo) : NotFound();
    [HttpPost] public async Task<IActionResult> Eliminar(int id) => await repositorio.Eliminar(id) ? RedirectToAction(nameof(Index)) : NotFound();
    private async Task<IActionResult> Guardar(TipoInmueble tipo, bool editar)
    { if (!ModelState.IsValid) return View(tipo); try { if (editar ? await repositorio.Actualizar(tipo) : await repositorio.Crear(tipo) > 0) return RedirectToAction(nameof(Index)); return NotFound(); } catch (InvalidOperationException ex) { ModelState.AddModelError(nameof(tipo.Descripcion), ex.Message); return View(tipo); } }
}
