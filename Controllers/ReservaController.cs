using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inmobiliaria.Controllers;

public class ReservaController(ReservaRepository repositorio, InquilinoRepository inquilinos, InmuebleRepository inmuebles) : Controller
{
    public async Task<IActionResult> Index() => View(await repositorio.ObtenerTodos());
    public async Task<IActionResult> Crear() { await CargarListas(); return View(new Reserva()); }
    [HttpPost] public async Task<IActionResult> Crear(Reserva reserva) => await Guardar(reserva, false);
    public async Task<IActionResult> Editar(int id) { var reserva=await repositorio.ObtenerPorId(id); if(reserva is null)return NotFound(); await CargarListas(); return View(reserva); }
    [HttpPost] public async Task<IActionResult> Editar(Reserva reserva) => await Guardar(reserva, true);
    public async Task<IActionResult> Detalles(int id) => await repositorio.ObtenerPorId(id) is { } reserva ? View(reserva) : NotFound();
    [HttpPost] public async Task<IActionResult> Eliminar(int id) => await repositorio.Eliminar(id) ? RedirectToAction(nameof(Index)) : NotFound();
    private async Task<IActionResult> Guardar(Reserva reserva, bool editar) { if (reserva.FechaFin < reserva.FechaInicio) ModelState.AddModelError(nameof(reserva.FechaFin), "La fecha de fin no puede ser anterior a la fecha de inicio."); if(!ModelState.IsValid){await CargarListas();return View(reserva);} if(editar ? await repositorio.Actualizar(reserva) : await repositorio.Crear(reserva)>0)return RedirectToAction(nameof(Index));return NotFound(); }
    private async Task CargarListas() { ViewBag.Inquilinos = new SelectList(await inquilinos.ObtenerTodos(1,1000), "Id", "Dni"); ViewBag.Inmuebles = new SelectList(await inmuebles.ObtenerTodos(), "Id", "Direccion"); }
}
