using Inmobiliaria.Models;
using MySqlConnector;

namespace Inmobiliaria.Repositories;

public class InmuebleRepository(IConfiguration config)
{
    private readonly string _cadenaConexion = config.GetConnectionString("DefaultConnection")!;
    private MySqlConnection CrearConexion() => new(_cadenaConexion);

    public async Task<List<Inmueble>> ObtenerTodos()
    {
        var inmuebles = new List<Inmueble>();
        await using var conexion = CrearConexion(); await conexion.OpenAsync();
        const string sql = "SELECT i.*, p.nombre propietario_nombre, p.apellido propietario_apellido, t.descripcion tipo_descripcion FROM inmuebles i JOIN propietarios p ON p.id = i.id_propietario JOIN tipos_inmueble t ON t.id = i.id_tipo WHERE i.estado = true ORDER BY i.direccion";
        await using var comando = new MySqlCommand(sql, conexion); await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync()) inmuebles.Add(Mapear(lector));
        return inmuebles;
    }

    public async Task<Inmueble?> ObtenerPorId(int id)
    {
        await using var conexion = CrearConexion(); await conexion.OpenAsync();
        const string sql = "SELECT i.*, p.nombre propietario_nombre, p.apellido propietario_apellido, t.descripcion tipo_descripcion FROM inmuebles i JOIN propietarios p ON p.id = i.id_propietario JOIN tipos_inmueble t ON t.id = i.id_tipo WHERE i.id = @id AND i.estado = true";
        await using var comando = new MySqlCommand(sql, conexion); comando.Parameters.AddWithValue("@id", id); await using var lector = await comando.ExecuteReaderAsync();
        return await lector.ReadAsync() ? Mapear(lector) : null;
    }

    public async Task<int> Crear(Inmueble inmueble)
    {
        await using var conexion = CrearConexion(); await conexion.OpenAsync();
        const string sql = "INSERT INTO inmuebles (id_propietario, id_tipo, direccion, cupo, coordenadas, precio_por_dia, porcentaje_reserva, imagen_portada, disponible, estado) VALUES (@propietario, @tipo, @direccion, @cupo, @coordenadas, @precio, @porcentaje, @imagen, @disponible, true); SELECT LAST_INSERT_ID();";
        await using var comando = new MySqlCommand(sql, conexion); CargarParametros(comando, inmueble); return Convert.ToInt32(await comando.ExecuteScalarAsync());
    }

    public async Task<bool> Actualizar(Inmueble inmueble)
    {
        await using var conexion = CrearConexion(); await conexion.OpenAsync();
        const string sql = "UPDATE inmuebles SET id_propietario=@propietario, id_tipo=@tipo, direccion=@direccion, cupo=@cupo, coordenadas=@coordenadas, precio_por_dia=@precio, porcentaje_reserva=@porcentaje, imagen_portada=@imagen, disponible=@disponible WHERE id=@id AND estado=true";
        await using var comando = new MySqlCommand(sql, conexion); comando.Parameters.AddWithValue("@id", inmueble.Id); CargarParametros(comando, inmueble); return await comando.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> Eliminar(int id)
    {
        await using var conexion = CrearConexion(); await conexion.OpenAsync(); const string sql = "UPDATE inmuebles SET estado=false WHERE id=@id";
        await using var comando = new MySqlCommand(sql, conexion); comando.Parameters.AddWithValue("@id", id); return await comando.ExecuteNonQueryAsync() > 0;
    }

    private static void CargarParametros(MySqlCommand c, Inmueble i)
    {
        c.Parameters.AddWithValue("@propietario", i.IdPropietario); c.Parameters.AddWithValue("@tipo", i.IdTipo); c.Parameters.AddWithValue("@direccion", i.Direccion); c.Parameters.AddWithValue("@cupo", i.Cupo);
        c.Parameters.AddWithValue("@coordenadas", (object?)i.Coordenadas ?? DBNull.Value); c.Parameters.AddWithValue("@precio", i.PrecioPorDia); c.Parameters.AddWithValue("@porcentaje", i.PorcentajeReserva);
        c.Parameters.AddWithValue("@imagen", (object?)i.ImagenPortada ?? DBNull.Value); c.Parameters.AddWithValue("@disponible", i.Disponible);
    }
    private static Inmueble Mapear(MySqlDataReader l) => new() { Id=l.GetInt32("id"), IdPropietario=l.GetInt32("id_propietario"), IdTipo=l.GetInt32("id_tipo"), Direccion=l.GetString("direccion"), Cupo=l.GetInt32("cupo"), Coordenadas=l["coordenadas"]?.ToString(), PrecioPorDia=l.GetDecimal("precio_por_dia"), PorcentajeReserva=l.GetDecimal("porcentaje_reserva"), ImagenPortada=l["imagen_portada"]?.ToString(), Disponible=l.GetBoolean("disponible"), Estado=l.GetBoolean("estado"), Propietario=new Propietario { Nombre=l.GetString("propietario_nombre"), Apellido=l.GetString("propietario_apellido") }, Tipo=new TipoInmueble { Descripcion=l.GetString("tipo_descripcion") } };
}
