using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Inmobiliaria.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inmobiliaria.Controllers;

public class ReservaController(
    ReservaRepository repositorio,
    InquilinoRepository inquilinos,
    InmuebleRepository inmuebles) : Controller
{
    public async Task<IActionResult> Index(
        int pagina = 1,
        int limite = 6)
    {
        limite = limite > 0 ? limite : 6;

        var cantidadTotal =
            await repositorio.ObtenerCantidad();

        var totalPaginas =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    (double)cantidadTotal / limite));

        pagina =
            Math.Clamp(
                pagina,
                1,
                totalPaginas);

        var registros =
            await repositorio.ObtenerTodos(
                pagina,
                limite);

        ViewData["PaginaActual"] = pagina;
        ViewData["Limite"] = limite;
        ViewData["CantidadTotal"] = cantidadTotal;
        ViewData["TieneSiguiente"] =
            pagina < totalPaginas;
        ViewData["TieneAnterior"] =
            pagina > 1;
        ViewData["TienePaginacion"] =
            cantidadTotal > limite;

        return View(registros);
    }

    public async Task<IActionResult> Crear()
    {
        await CargarListas();

        return View(new Reserva());
    }

    [HttpPost]
    public async Task<IActionResult> Crear(
        Reserva reserva,
        [ModelBinder(BinderType = typeof(ImportePagoBinder))]
        decimal? importeSena,
        DateTime? fechaPago)
    {
        ValidarFechas(reserva);

        var inmueble =
            await inmuebles.ObtenerPorId(
                reserva.IdInmueble);

        if (inmueble is null)
        {
            ModelState.AddModelError(
                nameof(reserva.IdInmueble),
                "El inmueble seleccionado no existe.");
        }
        else if (!inmueble.Disponible)
        {
            ModelState.AddModelError(
                nameof(reserva.IdInmueble),
                "El inmueble está suspendido y no admite nuevas reservas.");
        }

        if (!importeSena.HasValue)
        {
            ModelState.AddModelError(
                "importeSena",
                "Ingrese el importe de la seña.");
        }

        if (!fechaPago.HasValue)
        {
            ModelState.AddModelError(
                "fechaPago",
                "Ingrese la fecha del pago.");
        }

        if (ModelState.IsValid)
        {
            var superpuesta =
                await repositorio.ExisteSuperposicion(
                    reserva.IdInmueble,
                    reserva.FechaInicio,
                    reserva.FechaFin);

            if (superpuesta)
            {
                ModelState.AddModelError(
                    nameof(reserva.FechaInicio),
                    "El inmueble ya tiene una reserva que se superpone con las fechas seleccionadas.");
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                var id =
                    await repositorio.CrearConSena(
                        reserva,
                        importeSena!.Value,
                        fechaPago!.Value,
                        Seguridad.IdUsuario(User));

                return RedirectToAction(
                    nameof(Detalles),
                    new { id });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    ex.Message);
            }
            catch (MySqlConnector.MySqlException ex)
                when (ex.Number == 1452)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo guardar porque alguno de los registros seleccionados ya no existe.");
            }
        }

        await CargarListas(
            idInquilino: reserva.IdInquilino,
            idInmueble: reserva.IdInmueble);

        return View(reserva);
    }

    public async Task<IActionResult> Editar(int id)
    {
        var reserva =
            await repositorio.ObtenerPorId(id);

        if (reserva is null)
            return NotFound();

        if (reserva.FechaTerminacion.HasValue)
        {
            TempData["Mensaje"] =
                "Una reserva terminada no puede modificarse.";

            return RedirectToAction(
                nameof(Detalles),
                new { id });
        }

        await CargarListas(id);

        return View(reserva);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(
        Reserva reserva)
    {
        return await GuardarEdicion(reserva);
    }

    public async Task<IActionResult> Detalles(int id)
    {
        var reserva =
            await repositorio.ObtenerPorId(
                id,
                User.IsInRole(Roles.Administrador));

        return reserva is null
            ? NotFound()
            : View(reserva);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Eliminar(int id)
    {
        return await repositorio.Eliminar(id)
            ? RedirectToAction(nameof(Index))
            : NotFound();
    }

    public async Task<IActionResult> Renovar(int id)
    {
        var original =
            await repositorio.ObtenerPorId(id);

        if (original is null)
            return NotFound();

        var nueva = new Reserva
        {
            IdInquilino =
                original.IdInquilino,

            IdInmueble =
                original.IdInmueble,

            IdReservaOrigen =
                original.Id,

            FechaInicio =
                original.FechaFin,

            FechaFin =
                original.FechaFin.AddDays(1),

            MontoPorDia =
                original.MontoPorDia
        };

        return View(nueva);
    }

    [HttpPost]
    public async Task<IActionResult> Renovar(
        Reserva reserva,
        [ModelBinder(BinderType = typeof(ImportePagoBinder))]
        decimal? importeSena,
        DateTime? fechaPago)
    {
        if (!reserva.IdReservaOrigen.HasValue)
        {
            ModelState.AddModelError(
                string.Empty,
                "La renovación debe estar asociada a una reserva anterior.");
        }

        var original =
            reserva.IdReservaOrigen.HasValue
                ? await repositorio.ObtenerPorId(
                    reserva.IdReservaOrigen.Value)
                : null;

        if (original is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "La reserva original no existe.");
        }
        else
        {
            // No confiamos en los datos de la reserva enviados por el navegador.
            reserva.IdInquilino =
                original.IdInquilino;

            reserva.IdInmueble =
                original.IdInmueble;

            reserva.MontoPorDia =
                original.MontoPorDia;

            // MontoPorDia no forma parte del formulario de renovación.
            ModelState.Remove(nameof(Reserva.MontoPorDia));

            if (reserva.FechaInicio.Date != original.FechaFin.Date)
            {
                ModelState.AddModelError(
                    nameof(reserva.FechaInicio),
                    "La renovación debe comenzar en la fecha de finalización de la reserva original.");
            }
        }

        ValidarFechas(reserva);

        var inmueble =
            original is not null
                ? await inmuebles.ObtenerPorId(
                    original.IdInmueble)
                : null;

        if (inmueble is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "El inmueble no existe.");
        }
        else if (!inmueble.Disponible)
        {
            ModelState.AddModelError(
                string.Empty,
                "El inmueble está suspendido y no admite nuevas reservas.");
        }

        if (!importeSena.HasValue)
        {
            ModelState.AddModelError(
                "importeSena",
                "Ingrese la seña.");
        }

        if (!fechaPago.HasValue)
        {
            ModelState.AddModelError(
                "fechaPago",
                "Ingrese la fecha del pago.");
        }

        if (ModelState.IsValid)
        {
            var superpuesta =
                await repositorio.ExisteSuperposicion(
                    reserva.IdInmueble,
                    reserva.FechaInicio,
                    reserva.FechaFin);

            if (superpuesta)
            {
                ModelState.AddModelError(
                    nameof(reserva.FechaInicio),
                    "El inmueble ya está reservado durante esas fechas.");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewData["ImporteSena"] = importeSena;
            ViewData["FechaPago"] = fechaPago;
            return View(reserva);
        }

        try
        {
            var id =
                await repositorio.CrearConSena(
                    reserva,
                    importeSena!.Value,
                    fechaPago!.Value,
                    Seguridad.IdUsuario(User));

            return RedirectToAction(
                nameof(Detalles),
                new { id });
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            ViewData["ImporteSena"] = importeSena;
            ViewData["FechaPago"] = fechaPago;
            return View(reserva);
        }
    }

    public async Task<IActionResult> Terminar(int id)
    {
        var reserva =
            await repositorio.ObtenerPorId(id);

        if (reserva is null)
            return NotFound();

        if (reserva.FechaTerminacion.HasValue)
        {
            return RedirectToAction(
                nameof(Detalles),
                new { id });
        }

        if (reserva.FechaInicio.Date > DateTime.Today ||
            reserva.FechaFin.Date <= DateTime.Today)
        {
            TempData["Mensaje"] =
                "Solo se puede terminar anticipadamente una reserva actualmente vigente.";

            return RedirectToAction(
                nameof(Detalles),
                new { id });
        }

        return View(reserva);
    }

    [HttpPost]
    public async Task<IActionResult> Terminar(
        int id,
        DateTime fechaTerminacion)
    {
        var reserva =
            await repositorio.ObtenerPorId(id);

        if (reserva is null)
            return NotFound();

        try
        {
            var resultado =
                await repositorio.TerminarAnticipadamente(
                    id,
                    fechaTerminacion,
                    Seguridad.IdUsuario(User));

            if (!resultado)
                return NotFound();

            return RedirectToAction(
                nameof(Detalles),
                new { id });
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return View(reserva);
        }
    }

    public async Task<IActionResult> Vigentes()
    {
        return View(
            await repositorio.ObtenerVigentes());
    }

    public async Task<IActionResult> ProximasATerminar(
        int dias = 7)
    {
        if (dias < 0)
            dias = 7;

        ViewBag.Dias = dias;

        return View(
            await repositorio.ObtenerProximasATerminar(
                dias));
    }

    [HttpGet]
    public async Task<IActionResult> Opciones(
        string entidad,
        string? termino)
    {
        return entidad switch
        {
            "inquilinos" =>
                Json(await inquilinos.BuscarOpciones(termino)),
            "inmuebles" =>
                Json(await inmuebles.BuscarOpciones(entidad, termino)),
            _ => BadRequest()
        };
    }

    private async Task<IActionResult> GuardarEdicion(
        Reserva reserva)
    {
        ValidarFechas(reserva);

        var inmueble =
            await inmuebles.ObtenerPorId(
                reserva.IdInmueble);

        if (inmueble is null)
        {
            ModelState.AddModelError(
                nameof(reserva.IdInmueble),
                "El inmueble seleccionado no existe.");
        }

        /*
         * Una reserva ya existente no debe destruirse si
         * posteriormente el inmueble fue suspendido.
         *
         * Pero si al editar se intenta cambiar hacia otro
         * inmueble suspendido, la validación debe impedirlo.
         */
        var original =
            await repositorio.ObtenerPorId(
                reserva.Id);

        if (original?.FechaTerminacion.HasValue == true)
        {
            ModelState.AddModelError(
                string.Empty,
                "Una reserva terminada no puede modificarse.");
        }

        if (inmueble is not null &&
            !inmueble.Disponible &&
            original?.IdInmueble != inmueble.Id)
        {
            ModelState.AddModelError(
                nameof(reserva.IdInmueble),
                "El inmueble está suspendido y no admite nuevas reservas.");
        }

        if (ModelState.IsValid)
        {
            var superpuesta =
                await repositorio.ExisteSuperposicion(
                    reserva.IdInmueble,
                    reserva.FechaInicio,
                    reserva.FechaFin,
                    reserva.Id);

            if (superpuesta)
            {
                ModelState.AddModelError(
                    nameof(reserva.FechaInicio),
                    "El inmueble ya tiene una reserva que se superpone con las fechas seleccionadas.");
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (await repositorio.Actualizar(reserva))
                {
                    return RedirectToAction(
                        nameof(Detalles),
                        new { id = reserva.Id });
                }

                return NotFound();
            }
            catch (MySqlConnector.MySqlException ex)
                when (ex.Number == 1452)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo guardar porque uno de los registros seleccionados ya no existe.");
            }
        }

        await CargarListas(
            reserva.Id,
            reserva.IdInquilino,
            reserva.IdInmueble);

        return View("Editar", reserva);
    }

    private void ValidarFechas(Reserva reserva)
{
    if (reserva.FechaFin < reserva.FechaInicio)
    {
        ModelState.AddModelError(
            nameof(reserva.FechaFin),
            "La fecha de fin no puede ser anterior a la fecha de inicio.");
    }
}

    private async Task CargarListas(
        int idActual = 0,
        int idInquilino = 0,
        int idInmueble = 0)
    {
        Reserva? reserva = null;
        if (idActual > 0)
            reserva = await repositorio.ObtenerPorId(idActual);

        idInquilino = reserva?.IdInquilino ?? idInquilino;
        idInmueble = reserva?.IdInmueble ?? idInmueble;

        if (idInquilino > 0 || idInmueble > 0)
        {
            var inquilino =
                idInquilino > 0
                    ? await inquilinos.ObtenerPorId(idInquilino)
                    : null;

            var inmueble =
                idInmueble > 0
                    ? await inmuebles.ObtenerPorId(idInmueble)
                    : null;

            ViewBag.InquilinoSeleccionado = inquilino is null
                ? string.Empty
                : $"{inquilino.Apellido}, {inquilino.Nombre} (DNI {inquilino.Dni})";

            ViewBag.InmuebleSeleccionado =
                inmueble?.Direccion ?? string.Empty;
        }

        ViewBag.ReservasOrigen =
            await repositorio.ObtenerOpcionesOrigen(
                idActual);
    }
}
