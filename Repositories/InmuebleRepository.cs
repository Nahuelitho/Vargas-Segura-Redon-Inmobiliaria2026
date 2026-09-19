using Inmobiliaria.Models;
using MySqlConnector;

namespace Inmobiliaria.Repositories;

public class InmuebleRepository(IConfiguration config)
{
    private readonly string _cadenaConexion = config.GetConnectionString("DefaultConnection")!;
    private MySqlConnection CrearConexion() => new(_cadenaConexion);

    public async Task<int> ObtenerCantidad()
    {
        await using var conexion = CrearConexion();
        await conexion.OpenAsync();
        const string sql = "SELECT COUNT(*) FROM inmuebles i JOIN propietarios p ON p.id = i.id_propietario JOIN tipos_inmueble t ON t.id = i.id_tipo WHERE i.estado = true";
        await using var comando = new MySqlCommand(sql, conexion);
        return Convert.ToInt32(await comando.ExecuteScalarAsync());
    }

    public async Task<List<Inmueble>> ObtenerTodos(int paginaActual = 1, int? limite = null)
    {
        var inmuebles = new List<Inmueble>();
        await using var conexion = CrearConexion(); await conexion.OpenAsync();
        var sql = "SELECT i.*, p.nombre propietario_nombre, p.apellido propietario_apellido, t.descripcion tipo_descripcion FROM inmuebles i JOIN propietarios p ON p.id = i.id_propietario JOIN tipos_inmueble t ON t.id = i.id_tipo WHERE i.estado = true ORDER BY i.direccion, i.id";
        // Sin limite se conserva el listado completo para los selectores de los formularios.
        if (limite.HasValue) sql += " LIMIT @limit OFFSET @offset";
        await using var comando = new MySqlCommand(sql, conexion);
        if (limite.HasValue)
        {
            comando.Parameters.AddWithValue("@limit", limite.Value);
            comando.Parameters.AddWithValue("@offset", (long)(paginaActual - 1) * limite.Value);
        }
        await using var lector = await comando.ExecuteReaderAsync();
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

    public async Task<bool> CambiarDisponibilidad(int id, bool disponible)
    {
        await using var conexion = CrearConexion(); await conexion.OpenAsync();
        await using var comando = new MySqlCommand("UPDATE inmuebles SET disponible=@disponible WHERE id=@id AND estado=true", conexion);
        comando.Parameters.AddWithValue("@id", id); comando.Parameters.AddWithValue("@disponible", disponible);
        return await comando.ExecuteNonQueryAsync() > 0;
    }

    public async Task<(List<Inmueble> Inmuebles, int Total)> BuscarDisponibles(BusquedaInmueblesFiltro filtro, int pagina, int limite)
    {
        // La consulta se resuelve por completo en la base: no se cargan inmuebles para filtrarlos en memoria.
        const string desde = " FROM inmuebles i JOIN propietarios p ON p.id=i.id_propietario JOIN tipos_inmueble t ON t.id=i.id_tipo WHERE i.estado=true AND i.disponible=true AND NOT EXISTS (SELECT 1 FROM reservas r WHERE r.id_inmueble=i.id AND r.estado=true AND r.fecha_inicio<=@fin AND COALESCE(r.fecha_terminacion,r.fecha_fin)>=@inicio) AND (@tipo IS NULL OR i.id_tipo=@tipo) AND (@cupo IS NULL OR i.cupo>=@cupo) AND (@precio IS NULL OR i.precio_por_dia<=@precio) ";
        var total = 0;
        await using var conexion = CrearConexion(); await conexion.OpenAsync();
        await using (var cuenta = new MySqlCommand("SELECT COUNT(*)" + desde, conexion))
        {
            CargarFiltro(cuenta, filtro); total = Convert.ToInt32(await cuenta.ExecuteScalarAsync());
        }
        var inmuebles = new List<Inmueble>();
        await using var comando = new MySqlCommand("SELECT i.*,p.nombre propietario_nombre,p.apellido propietario_apellido,t.descripcion tipo_descripcion" + desde + "ORDER BY i.direccion,i.id LIMIT @limite OFFSET @offset", conexion);
        CargarFiltro(comando, filtro); comando.Parameters.AddWithValue("@limite", limite); comando.Parameters.AddWithValue("@offset", (long)(pagina - 1) * limite);
        await using var lector = await comando.ExecuteReaderAsync(); while (await lector.ReadAsync()) inmuebles.Add(Mapear(lector));
        return (inmuebles, total);
    }

    public async Task<(List<Inmueble> Inmuebles, int Total)> InformeGeneral(bool? disponible, int pagina, int limite)
    {
        var where = " WHERE i.estado=true" + (disponible.HasValue ? " AND i.disponible=@disponible" : "");
        var total = 0;
        await using var conexion = CrearConexion(); await conexion.OpenAsync();
        await using (var cuenta = new MySqlCommand("SELECT COUNT(*) FROM inmuebles i" + where, conexion)) { if (disponible.HasValue) cuenta.Parameters.AddWithValue("@disponible", disponible.Value); total = Convert.ToInt32(await cuenta.ExecuteScalarAsync()); }
        var lista = new List<Inmueble>(); await using var comando = new MySqlCommand("SELECT i.*,p.nombre propietario_nombre,p.apellido propietario_apellido,t.descripcion tipo_descripcion FROM inmuebles i JOIN propietarios p ON p.id=i.id_propietario JOIN tipos_inmueble t ON t.id=i.id_tipo" + where + " ORDER BY p.apellido,p.nombre,i.direccion LIMIT @limite OFFSET @offset", conexion);
        if (disponible.HasValue) comando.Parameters.AddWithValue("@disponible", disponible.Value); comando.Parameters.AddWithValue("@limite", limite); comando.Parameters.AddWithValue("@offset", (long)(pagina - 1) * limite);
        await using var lector = await comando.ExecuteReaderAsync(); while (await lector.ReadAsync()) lista.Add(Mapear(lector)); return (lista, total);
    }

    public async Task<List<Inmueble>> InformePorPropietario(int idPropietario)
    {
        var lista = new List<Inmueble>(); await using var conexion = CrearConexion(); await conexion.OpenAsync();
        await using var comando = new MySqlCommand("SELECT i.*,p.nombre propietario_nombre,p.apellido propietario_apellido,t.descripcion tipo_descripcion FROM inmuebles i JOIN propietarios p ON p.id=i.id_propietario JOIN tipos_inmueble t ON t.id=i.id_tipo WHERE i.estado=true AND i.id_propietario=@propietario ORDER BY i.direccion,i.id", conexion);
        comando.Parameters.AddWithValue("@propietario", idPropietario); await using var lector = await comando.ExecuteReaderAsync(); while (await lector.ReadAsync()) lista.Add(Mapear(lector)); return lista;
    }

    public async Task<List<InmuebleReservado>> MasReservados()
    {
        var lista = new List<InmuebleReservado>(); await using var conexion = CrearConexion(); await conexion.OpenAsync();
        const string sql = "SELECT i.*,p.nombre propietario_nombre,p.apellido propietario_apellido,t.descripcion tipo_descripcion,COUNT(r.id) cantidad_reservas FROM inmuebles i JOIN propietarios p ON p.id=i.id_propietario JOIN tipos_inmueble t ON t.id=i.id_tipo JOIN reservas r ON r.id_inmueble=i.id AND r.estado=true AND r.fecha_inicio>=DATE_SUB(CURDATE(),INTERVAL 365 DAY) WHERE i.estado=true GROUP BY i.id,p.nombre,p.apellido,t.descripcion ORDER BY cantidad_reservas DESC,i.direccion";
        await using var comando = new MySqlCommand(sql, conexion); await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync()) { var inmueble = Mapear(lector); lista.Add(new InmuebleReservado { Id=inmueble.Id, IdPropietario=inmueble.IdPropietario, Propietario=inmueble.Propietario, IdTipo=inmueble.IdTipo, Tipo=inmueble.Tipo, Direccion=inmueble.Direccion, Cupo=inmueble.Cupo, Coordenadas=inmueble.Coordenadas, PrecioPorDia=inmueble.PrecioPorDia, PorcentajeReserva=inmueble.PorcentajeReserva, ImagenPortada=inmueble.ImagenPortada, Disponible=inmueble.Disponible, Estado=inmueble.Estado, CantidadReservas=lector.GetInt32("cantidad_reservas") }); }
        return lista;
    }

    public async Task<List<Inmueble>> SinReservasDesde(int dias)
    {
        var lista = new List<Inmueble>(); await using var conexion = CrearConexion(); await conexion.OpenAsync();
        const string sql = "SELECT i.*,p.nombre propietario_nombre,p.apellido propietario_apellido,t.descripcion tipo_descripcion FROM inmuebles i JOIN propietarios p ON p.id=i.id_propietario JOIN tipos_inmueble t ON t.id=i.id_tipo WHERE i.estado=true AND NOT EXISTS (SELECT 1 FROM reservas r WHERE r.id_inmueble=i.id AND r.estado=true AND r.fecha_inicio>=DATE_SUB(CURDATE(),INTERVAL @dias DAY)) ORDER BY i.direccion,i.id";
        await using var comando = new MySqlCommand(sql, conexion); comando.Parameters.AddWithValue("@dias", dias); await using var lector = await comando.ExecuteReaderAsync(); while (await lector.ReadAsync()) lista.Add(Mapear(lector)); return lista;
    }

    public async Task<List<object>> BuscarOpciones(string entidad, string? termino)
    {
        termino = termino?.Trim(); if (termino?.Length > 80) termino = termino[..80];
        var patron = "%" + (termino ?? "").Replace("!", "!!").Replace("%", "!%").Replace("_", "!_") + "%";
        var sql = entidad == "tipos"
            ? "SELECT id,descripcion texto FROM tipos_inmueble WHERE estado=true AND descripcion LIKE @buscar ESCAPE '!' ORDER BY descripcion,id LIMIT 20"
            : "SELECT id,CONCAT(apellido,', ',nombre,' (DNI ',dni,')') texto FROM propietarios WHERE estado=true AND CONCAT(nombre,' ',apellido,' ',dni) LIKE @buscar ESCAPE '!' ORDER BY apellido,nombre,id LIMIT 20";
        var lista = new List<object>(); await using var conexion = CrearConexion(); await conexion.OpenAsync(); await using var comando = new MySqlCommand(sql, conexion); comando.Parameters.AddWithValue("@buscar", patron); await using var lector = await comando.ExecuteReaderAsync(); while (await lector.ReadAsync()) lista.Add(new { id=lector.GetInt32("id"), texto=lector.GetString("texto") }); return lista;
    }

    private static void CargarFiltro(MySqlCommand comando, BusquedaInmueblesFiltro filtro)
    {
        comando.Parameters.AddWithValue("@inicio", filtro.FechaInicio!.Value.Date); comando.Parameters.AddWithValue("@fin", filtro.FechaFin!.Value.Date);
        comando.Parameters.AddWithValue("@tipo", (object?)filtro.IdTipo ?? DBNull.Value); comando.Parameters.AddWithValue("@cupo", (object?)filtro.CupoMinimo ?? DBNull.Value); comando.Parameters.AddWithValue("@precio", (object?)filtro.PrecioMaximo ?? DBNull.Value);
    }

    private static void CargarParametros(MySqlCommand c, Inmueble i)
    {
        c.Parameters.AddWithValue("@propietario", i.IdPropietario); c.Parameters.AddWithValue("@tipo", i.IdTipo); c.Parameters.AddWithValue("@direccion", i.Direccion); c.Parameters.AddWithValue("@cupo", i.Cupo);
        c.Parameters.AddWithValue("@coordenadas", (object?)i.Coordenadas ?? DBNull.Value); c.Parameters.AddWithValue("@precio", (object?)i.PrecioPorDia ?? DBNull.Value); c.Parameters.AddWithValue("@porcentaje", (object?)i.PorcentajeReserva ?? DBNull.Value);
        c.Parameters.AddWithValue("@imagen", (object?)i.ImagenPortada ?? DBNull.Value); c.Parameters.AddWithValue("@disponible", i.Disponible);
    }
    private static Inmueble Mapear(MySqlDataReader l) => new() { Id=l.GetInt32("id"), IdPropietario=l.GetInt32("id_propietario"), IdTipo=l.GetInt32("id_tipo"), Direccion=l.GetString("direccion"), Cupo=l.GetInt32("cupo"), Coordenadas=l["coordenadas"]?.ToString(), PrecioPorDia=l.GetDecimal("precio_por_dia"), PorcentajeReserva=l.GetDecimal("porcentaje_reserva"), ImagenPortada=l["imagen_portada"]?.ToString(), Disponible=l.GetBoolean("disponible"), Estado=l.GetBoolean("estado"), Propietario=new Propietario { Nombre=l.GetString("propietario_nombre"), Apellido=l.GetString("propietario_apellido") }, Tipo=new TipoInmueble { Descripcion=l.GetString("tipo_descripcion") } };
}
