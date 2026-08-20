using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Inmobiliaria.Controllers;

public class InquilinoController(
    InquilinoRepository repository,
    ILogger<InquilinoController> logger
    ) : Controller
{
    private readonly InquilinoRepository _repository = repository;
    private readonly ILogger<InquilinoController> _logger = logger;

    public async Task<IActionResult> Index(int pagina = 1, int limite = 10)
    {
        var inquilinos = await _repository.ObtenerTodos(pagina, limite);

        ViewData["PaginaActual"] = pagina;
        ViewData["TieneSiguiente"] = inquilinos.Count() == limite;
        ViewData["TieneAnterior"] = pagina > 1;

        return View(inquilinos);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(new Inquilino());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Inquilino inquilino)
    {
        if (!ModelState.IsValid)
        {
            return View(inquilino);
        }

        try
        {
            var newInquilino = await _repository.Create(inquilino);
            if(newInquilino == null)
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
    public async Task<IActionResult> Update(int id)
    {
        var inquilino = await _repository.ObtenerPorId(id);
        if (inquilino == null)
        {
            return NotFound();
        }
        return View(inquilino);
    }

    [HttpPost]
    public async Task<IActionResult> Update(Inquilino inquilino)
    {
        if (!ModelState.IsValid) return View(inquilino);

        try
        {
            var update = await _repository.Update(inquilino);
            if (update)
            {
                return RedirectToAction(nameof(Index));
            }
            return NotFound();
        }
        catch (Exception e)
        {
            ModelState.AddModelError(string.Empty, e.Message);
        }

        return View(inquilino);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ok = await _repository.Delete(id);
            if (ok)
            {
                return RedirectToAction(nameof(Index));
            }
            return NotFound();
        }
        catch (Exception e)
        {
            ModelState.AddModelError(string.Empty, e.Message);
        }
        return View();
    }

}