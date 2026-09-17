using Microsoft.AspNetCore.Authorization;
using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Inmobiliaria.Controllers;

public class InquilinoController(
    InquilinoRepository repositorio,
    ILogger<InquilinoController> registrador
    ) : Controller
{
    private readonly InquilinoRepository _repositorio = repositorio;
    private readonly ILogger<InquilinoController> _registrador = registrador;

    public async Task<IActionResult> Index(int pagina = 1, int limite = 6)
    {
        limite = limite > 0 ? limite : 6;
        var cantidadTotal = await _repositorio.ObtenerCantidad();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling((double)cantidadTotal / limite));
        pagina = Math.Clamp(pagina, 1, totalPaginas);
        var registros = await _repositorio.ObtenerTodos(pagina, limite);

        ViewData["PaginaActual"] = pagina;
        ViewData["Limite"] = limite;
        ViewData["CantidadTotal"] = cantidadTotal;
        ViewData["TieneSiguiente"] = pagina < totalPaginas;
        ViewData["TieneAnterior"] = pagina > 1;
        ViewData["TienePaginacion"] = cantidadTotal > limite;
        return View(registros);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        return View(new Inquilino());
    }

    [HttpPost]
    public async Task<IActionResult> Crear(Inquilino inquilino)
    {
        if (!ModelState.IsValid)
        {
            return View(inquilino);
        }

        try
        {
            var nuevoInquilino = await _repositorio.Crear(inquilino);
            if(nuevoInquilino == null)
            {
                ModelState.AddModelError(string.Empty, "Error al crear el inquilino");
                return View(inquilino);
            }
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(inquilino.Dni), ex.Message);
            return View(inquilino);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var inquilino = await _repositorio.ObtenerPorId(id);
        if (inquilino == null)
        {
            return NotFound();
        }
        return View(inquilino);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(Inquilino inquilino)
    {
        if (!ModelState.IsValid) return View(inquilino);

        try
        {
            var actualizado = await _repositorio.Actualizar(inquilino);
            if (actualizado)
            {
                return RedirectToAction(nameof(Index));
            }
            return NotFound();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return View(inquilino);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            var exito = await _repositorio.Eliminar(id);
            if (exito)
            {
                return RedirectToAction(nameof(Index));
            }
            return NotFound();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        return View();
    }

}
