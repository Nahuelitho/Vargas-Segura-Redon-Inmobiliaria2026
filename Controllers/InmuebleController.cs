using Microsoft.AspNetCore.Authorization;
using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inmobiliaria.Controllers;

public class InmuebleController(InmuebleRepository repositorio, PropietarioRepository propietarios, TipoInmuebleRepository tipos) : Controller
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
    public async Task<IActionResult> Crear() { await CargarListas(); return View(new Inmueble()); }
    [HttpPost] public async Task<IActionResult> Crear(Inmueble inmueble) => await Guardar(inmueble, false);
    public async Task<IActionResult> Editar(int id) { var inmueble=await repositorio.ObtenerPorId(id); if(inmueble is null)return NotFound(); await CargarListas(); return View(inmueble); }
    [HttpPost] public async Task<IActionResult> Editar(Inmueble inmueble) => await Guardar(inmueble, true);
    public async Task<IActionResult> Detalles(int id) => await repositorio.ObtenerPorId(id) is { } inmueble ? View(inmueble) : NotFound();
    [HttpPost] [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Eliminar(int id) => await repositorio.Eliminar(id) ? RedirectToAction(nameof(Index)) : NotFound();
    private async Task<IActionResult> Guardar(Inmueble inmueble, bool editar) { if (!ModelState.IsValid) { await CargarListas(); return View(inmueble); } if (editar ? await repositorio.Actualizar(inmueble) : await repositorio.Crear(inmueble)>0) return RedirectToAction(nameof(Index)); return NotFound(); }
    private async Task CargarListas() { ViewBag.Propietarios = new SelectList(await propietarios.ObtenerTodos(1, 1000), "Id", "Nombre"); ViewBag.Tipos = new SelectList(await tipos.ObtenerTodos(), "Id", "Descripcion"); }
}
