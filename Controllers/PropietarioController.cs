using Microsoft.AspNetCore.Authorization;
using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Inmobiliaria.Controllers;

public class PropietarioController(PropietarioRepository repositorio, ILogger<PropietarioController> registrador): Controller
{
    private readonly PropietarioRepository _repositorio = repositorio;
    private readonly ILogger<PropietarioController> _registrador = registrador;

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
        return View(new Propietario());
    }

    [HttpPost]
    public async Task<IActionResult> Crear(Propietario propietario)
    {
      if(!ModelState.IsValid){
        return View(propietario);
      }

      try
      {
          await _repositorio.Crear(propietario);
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
    public async Task<IActionResult> Editar(int id)
    {
        var propietario = await _repositorio.ObtenerPorId(id);
        if (propietario == null)
        {
            return NotFound();
        }
        return View(propietario);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(Propietario propietario)
    {
        if (!ModelState.IsValid) return View(propietario);

        try
        {
            var actualizado = await _repositorio.Actualizar(propietario); // Update retorna bool
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

        return View(propietario);
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