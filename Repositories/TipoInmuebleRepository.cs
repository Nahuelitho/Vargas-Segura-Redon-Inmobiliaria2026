using Inmobiliaria.Models;
using MySqlConnector;

namespace Inmobiliaria.Repositories;

public class TipoInmuebleRepository(IConfiguration config)
{
    private readonly string _cadenaConexion = config.GetConnectionString("DefaultConnection")!;
    private MySqlConnection CrearConexion() => new(_cadenaConexion);

    public async Task<List<TipoInmueble>> ObtenerTodos()
    {
        var tipos = new List<TipoInmueble>();
        await using var conexion = CrearConexion(); await conexion.OpenAsync();
        const string sql = "SELECT id, descripcion, estado FROM tipos_inmueble WHERE estado = true ORDER BY descripcion";
        await using var comando = new MySqlCommand(sql, conexion); await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync()) tipos.Add(new TipoInmueble { Id = lector.GetInt32("id"), Descripcion = lector.GetString("descripcion"), Estado = lector.GetBoolean("estado") });
        return tipos;
    }

    public async Task<TipoInmueble?> ObtenerPorId(int id)
    {
        await using var conexion = CrearConexion(); await conexion.OpenAsync();
        const string sql = "SELECT id, descripcion, estado FROM tipos_inmueble WHERE id = @id AND estado = true";
        await using var comando = new MySqlCommand(sql, conexion); comando.Parameters.AddWithValue("@id", id); await using var lector = await comando.ExecuteReaderAsync();
        return await lector.ReadAsync() ? new TipoInmueble { Id = lector.GetInt32("id"), Descripcion = lector.GetString("descripcion"), Estado = lector.GetBoolean("estado") } : null;
    }

    public async Task<int> Crear(TipoInmueble tipo)
    {
        await using var conexion = CrearConexion(); await conexion.OpenAsync();
        const string sql = "INSERT INTO tipos_inmueble (descripcion, estado) VALUES (@descripcion, true); SELECT LAST_INSERT_ID();";
        await using var comando = new MySqlCommand(sql, conexion); comando.Parameters.AddWithValue("@descripcion", tipo.Descripcion);
        try { return Convert.ToInt32(await comando.ExecuteScalarAsync()); }
        catch (MySqlException ex) when (ex.Number == 1062) { throw new InvalidOperationException("Ya existe un tipo de inmueble con esa descripción.", ex); }
    }

    public async Task<bool> Actualizar(TipoInmueble tipo)
    {
        await using var conexion = CrearConexion(); await conexion.OpenAsync();
        const string sql = "UPDATE tipos_inmueble SET descripcion = @descripcion WHERE id = @id AND estado = true";
        await using var comando = new MySqlCommand(sql, conexion); comando.Parameters.AddWithValue("@id", tipo.Id); comando.Parameters.AddWithValue("@descripcion", tipo.Descripcion);
        try { return await comando.ExecuteNonQueryAsync() > 0; }
        catch (MySqlException ex) when (ex.Number == 1062) { throw new InvalidOperationException("Ya existe un tipo de inmueble con esa descripción.", ex); }
    }

    public async Task<bool> Eliminar(int id)
    {
        await using var conexion = CrearConexion(); await conexion.OpenAsync();
        const string sql = "UPDATE tipos_inmueble SET estado = false WHERE id = @id";
        await using var comando = new MySqlCommand(sql, conexion); comando.Parameters.AddWithValue("@id", id); return await comando.ExecuteNonQueryAsync() > 0;
    }
}
