using Inmobiliaria.Models;
using MySqlConnector;

namespace Inmobiliaria.Repositories;

public class ImagenInmuebleRepository(IConfiguration config)
{
    private readonly string cadenaConexion = config.GetConnectionString("DefaultConnection")!;

    public async Task<List<ImagenInmueble>> ObtenerPorInmueble(int idInmueble)
    {
        var imagenes = new List<ImagenInmueble>();
        await using var conexion = new MySqlConnection(cadenaConexion);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand("SELECT id,id_inmueble,url,estado FROM imagenes_inmueble WHERE id_inmueble=@inmueble AND estado=true ORDER BY id", conexion);
        comando.Parameters.AddWithValue("@inmueble", idInmueble);
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            imagenes.Add(new ImagenInmueble { Id = lector.GetInt32("id"), IdInmueble = lector.GetInt32("id_inmueble"), Url = lector.GetString("url"), Estado = lector.GetBoolean("estado") });
        return imagenes;
    }

    public async Task<bool> Crear(ImagenInmueble imagen)
    {
        await using var conexion = new MySqlConnection(cadenaConexion);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand("INSERT INTO imagenes_inmueble (id_inmueble,url,estado) SELECT id,@url,true FROM inmuebles WHERE id=@inmueble AND estado=true", conexion);
        comando.Parameters.AddWithValue("@inmueble", imagen.IdInmueble);
        comando.Parameters.AddWithValue("@url", imagen.Url.Trim());
        return await comando.ExecuteNonQueryAsync() > 0;
    }

    public async Task<string?> ObtenerNombreArchivo(int id, int idInmueble)
    {
        await using var conexion = new MySqlConnection(cadenaConexion);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand(
            "SELECT url FROM imagenes_inmueble WHERE id=@id AND id_inmueble=@inmueble AND estado=true",
            conexion);
        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@inmueble", idInmueble);
        var resultado = await comando.ExecuteScalarAsync();
        return resultado?.ToString();
    }

    public async Task<bool> Eliminar(int id, int idInmueble)
    {
        await using var conexion = new MySqlConnection(cadenaConexion);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand("UPDATE imagenes_inmueble SET estado=false WHERE id=@id AND id_inmueble=@inmueble AND estado=true", conexion);
        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@inmueble", idInmueble);
        return await comando.ExecuteNonQueryAsync() > 0;
    }
}
