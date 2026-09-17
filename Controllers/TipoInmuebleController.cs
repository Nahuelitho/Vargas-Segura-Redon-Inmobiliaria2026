using Microsoft.AspNetCore.Authorization;
using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Inmobiliaria.Controllers;

public class TipoInmuebleController(TipoInmuebleRepository repositorio) : Controller
{
    public async Task<IActionResult> Index(int pagina = 1, int limite = 6)
    {
        limite = limite > 0 ? limite : 6;
        var cantidadTotal = await repositorio.ObtenerCantidad();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling((double)cantidadTotal / limite));
        pagina = Math.Clamp(pagina, 1, totalPaginas);
        var registros = await repositorio.ObtenerTodos(pagina, limite);

        ViewData["PaginaActual"] = pagina;
        ViewData["Limite"] = limite;
        ViewData["CantidadTotal"] = cantidadTotal;
        ViewData["TieneSiguiente"] = pagina < totalPaginas;
        ViewData["TieneAnterior"] = pagina > 1;
        ViewData["TienePaginacion"] = cantidadTotal > limite;
        return View(registros);
    }
    public IActionResult Crear() => View(new TipoInmueble());
    [HttpPost] public async Task<IActionResult> Crear(TipoInmueble tipo) => await Guardar(tipo, false);
    public async Task<IActionResult> Editar(int id) => await repositorio.ObtenerPorId(id) is { } tipo ? View(tipo) : NotFound();
    [HttpPost] public async Task<IActionResult> Editar(TipoInmueble tipo) => await Guardar(tipo, true);
    public async Task<IActionResult> Detalles(int id) => await repositorio.ObtenerPorId(id) is { } tipo ? View(tipo) : NotFound();
    [HttpPost] [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Eliminar(int id) => await repositorio.Eliminar(id) ? RedirectToAction(nameof(Index)) : NotFound();
    private async Task<IActionResult> Guardar(TipoInmueble tipo, bool editar)
    { if (!ModelState.IsValid) return View(tipo); try { if (editar ? await repositorio.Actualizar(tipo) : await repositorio.Crear(tipo) > 0) return RedirectToAction(nameof(Index)); return NotFound(); } catch (InvalidOperationException ex) { ModelState.AddModelError(nameof(tipo.Descripcion), ex.Message); return View(tipo); } }
}
