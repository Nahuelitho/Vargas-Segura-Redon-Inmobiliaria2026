using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Inmobiliaria.Controllers;

public class PropietarioController : Controller
{
    private readonly PropietarioRepository _repository;
    private readonly ILogger<PropietarioController> _logger;

    public PropietarioController(PropietarioRepository repository, ILogger<PropietarioController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var propietarios = await _repository.ObtenerTodos();
        return View(propietarios);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
      return View(new Propietario());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Propietario propietario)
    {
      if(!ModelState.IsValid){
        return View(propietario);
      }

      try
      {
          await _repository.Create(propietario);
          return RedirectToAction(nameof(Index));
      }
      catch (InvalidOperationException ex)
      {
          // Ej: DNI duplicado — mostrar el error en el formulario
          ModelState.AddModelError(nameof(propietario.Dni), ex.Message);
          return View(propietario);
      }
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        var propietario = await _repository.ObtenerPorId(id);
        if (propietario == null)
        {
            return NotFound();
        }
        return View(propietario);
    }

    [HttpPost]
    public async Task<IActionResult> Update(Propietario propietario)
    {
        if (!ModelState.IsValid) return View(propietario);

        try
        {
            var update = await _repository.Update(propietario); // Update retorna bool
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

        return View(propietario);
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