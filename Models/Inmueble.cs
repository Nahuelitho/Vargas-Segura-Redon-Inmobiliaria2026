namespace Inmobiliaria.Models;

public class Inmueble
{
    public int Id { get; set; }

    public int IdPropietario { get; set; }
    public Propietario? Propietario { get; set; }

    public int IdTipo { get; set; }
    public TipoInmueble? Tipo { get; set; }

    public string Direccion { get; set; } = "";
    public int Cupo { get; set; }
    public string? Coordenadas { get; set; }
    public decimal PrecioPorDia { get; set; }
    public decimal PorcentajeReserva { get; set; }
    public string? ImagenPortada { get; set; }
    public bool Disponible { get; set; } = true;
    public bool Estado { get; set; } = true;
}