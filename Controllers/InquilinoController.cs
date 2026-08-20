using Inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Inmobiliaria.Controllers;

public class InquilinoController : Controller
{
    private readonly InquilinoRepository _repository;
    private readonly ILogger<InquilinoController> _logger;

    public InquilinoController(InquilinoRepository repository, ILogger<InquilinoController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var inquilinos = await _repository.ObtenerTodos();

        return View(inquilinos);
    }
}