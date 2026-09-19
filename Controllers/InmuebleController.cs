using Microsoft.AspNetCore.Authorization;
using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Inmobiliaria.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inmobiliaria.Controllers;

public class InmuebleController(
    InmuebleRepository repositorio,
    ImagenInmuebleRepository imagenesRepo,
    ImagenInmuebleService imagenesServicio) : Controller
{
    public async Task<IActionResult> Index(int pagina = 1, int limite = 6)
    {
        limite = Math.Clamp(limite, 1, 100);
        var cantidadTotal = await repositorio.ObtenerCantidad();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling((double)cantidadTotal / limite));
        pagina = Math.Clamp(pagina, 1, totalPaginas);
        var registros = await repositorio.ObtenerTodos(pagina, limite);
        PrepararPaginacion(pagina, limite, cantidadTotal);
        return View(registros);
    }

    public IActionResult Crear() => View(new Inmueble());

    [HttpPost]
    public async Task<IActionResult> Crear(Inmueble inmueble) => await Guardar(inmueble, false);

    public async Task<IActionResult> Editar(int id) =>
        await repositorio.ObtenerPorId(id) is { } inmueble ? View(inmueble) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Editar(Inmueble inmueble) => await Guardar(inmueble, true);

    public async Task<IActionResult> Detalles(int id) =>
        await repositorio.ObtenerPorId(id) is { } inmueble ? View(inmueble) : NotFound();

    [HttpPost, Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Eliminar(int id)
    {
        var inmueble = await repositorio.ObtenerPorId(id);
        if (inmueble is null) return NotFound();
        // Borrar archivo de portada si es local
        imagenesServicio.Eliminar(inmueble.ImagenPortada);
        // Borrar archivos de galeria
        var galeria = await imagenesRepo.ObtenerPorInmueble(id);
        foreach (var img in galeria) imagenesServicio.Eliminar(img.Url);
        if (!await repositorio.Eliminar(id)) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CambiarDisponibilidad(int id, bool disponible)
    {
        if (!await repositorio.CambiarDisponibilidad(id, disponible)) return NotFound();
        TempData["Mensaje"] = disponible
            ? "La oferta del inmueble fue reactivada."
            : "La oferta del inmueble fue suspendida. Las reservas existentes se conservaron.";
        return RedirectToAction(nameof(Detalles), new { id });
    }

    // ── Galeria ─────────────────────────────────────────────────────────────

    public async Task<IActionResult> Galeria(int id)
    {
        var inmueble = await repositorio.ObtenerPorId(id);
        if (inmueble is null) return NotFound();
        return View(new GaleriaInmuebleVista
        {
            Inmueble = inmueble,
            Imagenes = await imagenesRepo.ObtenerPorInmueble(id),
            NuevaImagen = new ImagenInmueble { IdInmueble = id }
        });
    }

    [HttpPost]
    public async Task<IActionResult> AgregarImagen(ImagenInmueble imagen)
    {
        // El unico campo que valida el usuario es Archivo
        if (!ModelState.IsValid)
        {
            var inmueble = await repositorio.ObtenerPorId(imagen.IdInmueble);
            if (inmueble is null) return NotFound();
            return View("Galeria", new GaleriaInmuebleVista
            {
                Inmueble = inmueble,
                Imagenes = await imagenesRepo.ObtenerPorInmueble(imagen.IdInmueble),
                NuevaImagen = imagen
            });
        }

        string nombreArchivo;
        try { nombreArchivo = await imagenesServicio.Guardar(imagen.Archivo!); }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("Archivo", ex.Message);
            var inmueble = await repositorio.ObtenerPorId(imagen.IdInmueble);
            if (inmueble is null) return NotFound();
            return View("Galeria", new GaleriaInmuebleVista
            {
                Inmueble = inmueble,
                Imagenes = await imagenesRepo.ObtenerPorInmueble(imagen.IdInmueble),
                NuevaImagen = imagen
            });
        }

        imagen.Url = nombreArchivo;
        if (!await imagenesRepo.Crear(imagen))
        {
            imagenesServicio.Eliminar(nombreArchivo);
            return NotFound();
        }

        TempData["Mensaje"] = "Imagen agregada a la galeria.";
        return RedirectToAction(nameof(Galeria), new { id = imagen.IdInmueble });
    }

    [HttpPost, Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> EliminarImagen(int id, int idInmueble)
    {
        var nombre = await imagenesRepo.ObtenerNombreArchivo(id, idInmueble);
        if (!await imagenesRepo.Eliminar(id, idInmueble)) return NotFound();
        imagenesServicio.Eliminar(nombre);
        TempData["Mensaje"] = "Imagen retirada de la galeria.";
        return RedirectToAction(nameof(Galeria), new { id = idInmueble });
    }

    // ── Busqueda ─────────────────────────────────────────────────────────────

    public IActionResult Buscar() => View(new BusquedaInmueblesFiltro());

    [HttpPost]
    public async Task<IActionResult> Buscar(BusquedaInmueblesFiltro filtro, int pagina = 1)
    {
        if (!ModelState.IsValid) return View(filtro);
        const int limite = 6;
        var resultado = await repositorio.BuscarDisponibles(filtro, Math.Max(1, pagina), limite);
        var paginas = Math.Max(1, (int)Math.Ceiling((double)resultado.Total / limite));
        ViewBag.Resultado = new ListadoInmueblesVista
        {
            Inmuebles = resultado.Inmuebles,
            Pagina = Math.Min(pagina, paginas),
            TotalPaginas = paginas,
            Total = resultado.Total
        };
        return View(filtro);
    }

    // ── Informes ─────────────────────────────────────────────────────────────

    public async Task<IActionResult> InformeGeneral(bool? disponible, int pagina = 1)
    {
        const int limite = 10;
        var resultado = await repositorio.InformeGeneral(disponible, Math.Max(1, pagina), limite);
        var paginas = Math.Max(1, (int)Math.Ceiling((double)resultado.Total / limite));
        ViewBag.Disponible = disponible;
        ViewBag.Pagina = Math.Min(pagina, paginas);
        ViewBag.TotalPaginas = paginas;
        return View(resultado.Inmuebles);
    }

    public async Task<IActionResult> InformePorPropietario(int? idPropietario)
    {
        ViewBag.IdPropietario = idPropietario;
        ViewBag.Inmuebles = idPropietario.HasValue
            ? await repositorio.InformePorPropietario(idPropietario.Value)
            : new List<Inmueble>();
        return View();
    }

    public async Task<IActionResult> MasReservados() => View(await repositorio.MasReservados());

    public async Task<IActionResult> SinReservas(int dias = 30)
    {
        dias = Math.Clamp(dias, 1, 3650);
        ViewBag.Dias = dias;
        return View(await repositorio.SinReservasDesde(dias));
    }

    public async Task<IActionResult> Libres(DateTime? fechaInicio, DateTime? fechaFin)
    {
        var filtro = new BusquedaInmueblesFiltro { FechaInicio = fechaInicio, FechaFin = fechaFin };
        if (!fechaInicio.HasValue && !fechaFin.HasValue) return View(filtro);
        if (!TryValidateModel(filtro)) return View(filtro);
        var resultado = await repositorio.BuscarDisponibles(filtro, 1, 100);
        ViewBag.Inmuebles = resultado.Inmuebles;
        return View(filtro);
    }

    // ── Opciones (select2 server-side) ───────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Opciones(string entidad, string? termino)
    {
        if (entidad is not ("tipos" or "propietarios")) return BadRequest();
        return Json(await repositorio.BuscarOpciones(entidad, termino));
    }

    // ── Privados ─────────────────────────────────────────────────────────────

    private async Task<IActionResult> Guardar(Inmueble inmueble, bool editar)
    {
        // Ignorar validacion de ArchivoPortada para el model binding
        ModelState.Remove("ArchivoPortada");

        if (!ModelState.IsValid) return View(editar ? "Editar" : "Crear", inmueble);

        // Procesar imagen de portada si se subio una nueva
        if (inmueble.ArchivoPortada is { Length: > 0 })
        {
            try
            {
                // Si es edicion, borrar la imagen anterior
                if (editar)
                {
                    var anterior = await repositorio.ObtenerPorId(inmueble.Id);
                    imagenesServicio.Eliminar(anterior?.ImagenPortada);
                }
                inmueble.ImagenPortada = await imagenesServicio.Guardar(inmueble.ArchivoPortada);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("ArchivoPortada", ex.Message);
                return View(editar ? "Editar" : "Crear", inmueble);
            }
        }
        else if (editar)
        {
            // Sin nueva imagen en edicion: recuperar el nombre existente para no pisarlo
            var existente = await repositorio.ObtenerPorId(inmueble.Id);
            inmueble.ImagenPortada = existente?.ImagenPortada;
        }

        if (editar ? await repositorio.Actualizar(inmueble) : await repositorio.Crear(inmueble) > 0)
            return RedirectToAction(nameof(Index));
        return NotFound();
    }

    private void PrepararPaginacion(int pagina, int limite, int cantidad)
    {
        ViewData["PaginaActual"] = pagina;
        ViewData["Limite"] = limite;
        ViewData["CantidadTotal"] = cantidad;
        ViewData["TieneSiguiente"] = pagina * limite < cantidad;
        ViewData["TieneAnterior"] = pagina > 1;
        ViewData["TienePaginacion"] = cantidad > limite;
    }
}
