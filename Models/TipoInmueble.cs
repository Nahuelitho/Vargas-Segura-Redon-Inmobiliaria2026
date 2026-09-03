using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Models;

public class TipoInmueble
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = "";
    public bool Estado { get; set; } = true;

    public List<Inmueble> Inmuebles { get; set; } = new();
}