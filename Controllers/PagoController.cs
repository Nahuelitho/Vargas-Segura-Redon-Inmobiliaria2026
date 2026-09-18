using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Inmobiliaria.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inmobiliaria.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PagoController(PagoRepository pagos, ReservaRepository reservas, InmuebleRepository inmuebles) : Controller
{
    public async Task<IActionResult> Index(int idReserva)
    {
        var reserva = await reservas.ObtenerPorId(idReserva);
        if (reserva is null) return NotFound();
        var inmueble = await inmuebles.ObtenerPorId(reserva.IdInmueble);
        var vista = new PagosReservaVista { Reserva = reserva, Pagos = await pagos.ObtenerPorReserva(idReserva, User.IsInRole(Roles.Administrador)) };
        try { vista.SenaEsperada = SenaService.Calcular(reserva, inmueble?.PorcentajeReserva); }
        catch (ArgumentException) { /* Los datos históricos incompletos no impiden consultar pagos. */ }
        return View(vista);
    }

    public async Task<IActionResult> Crear(int idReserva)
    {
        if (await reservas.ObtenerPorId(idReserva) is null) return NotFound();
        ViewBag.IdReserva = idReserva;
        return View(new PagoFormulario());
    }

    [HttpPost]
    public async Task<IActionResult> Crear(int idReserva, PagoFormulario formulario)
    {
        if (await reservas.ObtenerPorId(idReserva) is null) return NotFound();
        ViewBag.IdReserva = idReserva;
        if (!ModelState.IsValid) return View(formulario);
        if (await pagos.Crear(idReserva, formulario, Seguridad.IdUsuario(User)) <= 0) return NotFound();
        return RedirectToAction(nameof(Index), new { idReserva });
    }

    public async Task<IActionResult> Editar(int id, int idReserva)
    {
        if (await reservas.ObtenerPorId(idReserva) is null) return NotFound();
        var pago = (await pagos.ObtenerPorReserva(idReserva)).SingleOrDefault(p => p.Id == id && p.Estado);
        if (pago is null) return NotFound();
        ViewBag.IdReserva = idReserva; ViewBag.IdPago = id;
        return View(new PagoConceptoFormulario { Concepto = pago.Concepto });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, int idReserva, PagoConceptoFormulario formulario)
    {
        if (await reservas.ObtenerPorId(idReserva) is null) return NotFound();
        ViewBag.IdReserva = idReserva; ViewBag.IdPago = id;
        if (!ModelState.IsValid) return View(formulario);
        if (!await pagos.EditarConcepto(id, idReserva, formulario.Concepto)) return NotFound();
        return RedirectToAction(nameof(Index), new { idReserva });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Anular(int id, int idReserva)
    {
        if (await reservas.ObtenerPorId(idReserva) is null) return NotFound();
        if (!await pagos.Anular(id, idReserva, Seguridad.IdUsuario(User))) return NotFound();
        return RedirectToAction(nameof(Index), new { idReserva });
    }
}
