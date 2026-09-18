using Microsoft.AspNetCore.Authorization;
using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inmobiliaria.Controllers;

public class ReservaController(ReservaRepository repositorio, InquilinoRepository inquilinos, InmuebleRepository inmuebles) : Controller
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
    public async Task<IActionResult> Crear() { await CargarListas(); return View(new Reserva()); }
    [HttpPost] public async Task<IActionResult> Crear(Reserva reserva) => await Guardar(reserva, false);
    public async Task<IActionResult> Editar(int id) { var reserva=await repositorio.ObtenerPorId(id); if(reserva is null)return NotFound(); await CargarListas(id); return View(reserva); }
    [HttpPost] public async Task<IActionResult> Editar(Reserva reserva) => await Guardar(reserva, true);
    public async Task<IActionResult> Detalles(int id) => await repositorio.ObtenerPorId(id) is { } reserva ? View(reserva) : NotFound();
    [HttpPost] [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Eliminar(int id) => await repositorio.Eliminar(id) ? RedirectToAction(nameof(Index)) : NotFound();
    private async Task<IActionResult> Guardar(Reserva reserva, bool editar)
    {
        if (reserva.FechaFin < reserva.FechaInicio)
            ModelState.AddModelError(nameof(reserva.FechaFin), "La fecha de fin no puede ser anterior a la fecha de inicio.");

        if (reserva.IdReservaOrigen.HasValue)
        {
            if (editar && reserva.IdReservaOrigen == reserva.Id)
                ModelState.AddModelError(nameof(reserva.IdReservaOrigen), "Una reserva no puede ser su propio origen.");
            else if (!await repositorio.Existe(reserva.IdReservaOrigen.Value))
                ModelState.AddModelError(nameof(reserva.IdReservaOrigen), "La reserva de origen no existe. Seleccione una de la lista o elija Sin reserva de origen.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (editar ? await repositorio.Actualizar(reserva) : await repositorio.Crear(reserva) > 0)
                    return RedirectToAction(nameof(Index));
                return NotFound();
            }
            catch (MySqlConnector.MySqlException ex) when (ex.Number == 1452)
            {
                ModelState.AddModelError(string.Empty, "No se pudo guardar porque uno de los registros seleccionados ya no existe. Revise el inquilino, el inmueble y la reserva de origen.");
            }
        }
        await CargarListas(editar ? reserva.Id : 0);
        return View(reserva);
    }

    private async Task CargarListas(int idActual = 0)
    {
        ViewBag.Inquilinos = (await inquilinos.ObtenerTodos(1,1000))
            .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = $"{i.Nombre} {i.Apellido}" })
            .ToList();
        ViewBag.Inmuebles = new SelectList(await inmuebles.ObtenerTodos(), "Id", "Direccion");
        ViewBag.ReservasOrigen = await repositorio.ObtenerOpcionesOrigen(idActual);
    }
}
