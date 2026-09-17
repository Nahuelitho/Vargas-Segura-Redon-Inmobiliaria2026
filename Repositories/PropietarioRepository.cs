using System.Data;
using Inmobiliaria.Models;
using MySqlConnector;

namespace Inmobiliaria.Repositories;

public class PropietarioRepository(IConfiguration config)
{
  private readonly string _cadenaConexion = config.GetConnectionString("DefaultConnection")!;

  private MySqlConnection CrearConexion()
  {
    return new MySqlConnection(_cadenaConexion);
  }

  // LISTAR
  public async Task<List<Propietario>> ObtenerTodos(int paginaActual = 1, int limite = 10)
  {
        var propietarios = new List<Propietario>();

        await using var conexion = CrearConexion();
        await conexion.OpenAsync();

        const string consultaSql = """
        SELECT Id, Dni, Nombre, Apellido, Telefono, Email, Direccion, Estado
        FROM Propietarios
        WHERE Estado = true
        LIMIT @limit OFFSET @offset
        """;

        await using var comando = new MySqlCommand(consultaSql, conexion);
        comando.Parameters.AddWithValue("@limit", limite);
        comando.Parameters.AddWithValue("@offset", (paginaActual - 1) * limite);
        await using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            propietarios.Add(new Propietario
            {
                Id       = lector.GetInt32("id"),
                Dni      = lector.GetString("dni"),
                Nombre   = lector.GetString("nombre"),
                Apellido = lector.GetString("apellido"),
                Telefono = lector["telefono"]?.ToString() ?? "",
                Email    = lector["email"]?.ToString() ?? "",
                Direccion= lector["direccion"]?.ToString() ?? "",
                Estado   = lector.GetBoolean("estado")
            });
        }

        return propietarios;
    }
    // CONTAR PROPIETARIOS
public async Task<int> ObtenerCantidad()
{
    await using var conexion = CrearConexion();
    await conexion.OpenAsync();

    const string consultaSql = """
        SELECT COUNT(*)
        FROM Propietarios
        WHERE Estado = true;
        """;

    await using var comando = new MySqlCommand(consultaSql, conexion);

    var resultado = await comando.ExecuteScalarAsync();

    return Convert.ToInt32(resultado);
}

    // Obtener un propietario por id
    public async Task<List<OpcionBusqueda>> Buscar(string? termino)
    {
        termino = termino?.Trim();
        if (string.IsNullOrEmpty(termino) || termino.Length < 2 || termino.Length > 100)
            return new List<OpcionBusqueda>();

        await using var conexion = CrearConexion();
        await conexion.OpenAsync();
        const string sql = "SELECT id, CONCAT(apellido, ', ', nombre, ' - DNI ', dni) AS texto FROM propietarios WHERE estado = true AND (dni LIKE @termino ESCAPE '!' OR CONCAT(nombre, ' ', apellido) LIKE @termino ESCAPE '!' OR CONCAT(apellido, ' ', nombre) LIKE @termino ESCAPE '!') ORDER BY apellido, nombre, id LIMIT 21";
        await using var comando = new MySqlCommand(sql, conexion);
        var filtro = termino.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_");
        comando.Parameters.AddWithValue("@termino", $"%{filtro}%");
        await using var lector = await comando.ExecuteReaderAsync();
        var resultados = new List<OpcionBusqueda>();
        while (await lector.ReadAsync())
            resultados.Add(new OpcionBusqueda(lector.GetInt32("id"), lector.GetString("texto")));
        return resultados;
    }
    public async Task<Propietario?> ObtenerPorId(int id)
    {
      await using var conexion = CrearConexion();
      await conexion.OpenAsync();

      const string consultaSql = """
        SELECT * FROM propietarios WHERE id = @Id AND estado = true;
      """;

        await using var comando = new MySqlCommand(consultaSql, conexion);
        comando.Parameters.AddWithValue("@Id", id);

        await using var lector = await comando.ExecuteReaderAsync();

        if(await lector.ReadAsync()){
            return new Propietario{
                Id = lector.GetInt32("id"),
                Dni = lector.GetString("dni"),
                Nombre = lector.GetString("nombre"),
                Apellido = lector.GetString("apellido"),
                Telefono = lector["telefono"]?.ToString() ?? "",
                Email = lector["email"]?.ToString() ?? "",
                Direccion = lector["direccion"]?.ToString() ?? "",
                Estado = lector.GetBoolean("estado")
            };
        }
        return null;
    }

    //CREAR
    public async Task<Propietario?> Crear(Propietario propietario)
    {
        await using var conexion = CrearConexion();
        await conexion.OpenAsync();

        const string consultaSql = """ 
          INSERT INTO propietarios (dni, nombre, apellido, telefono, email, direccion, estado)
          VALUES (@Dni, @Nombre, @Apellido, @Telefono, @Email, @Direccion, 1);
          SELECT last_insert_id();
         """;

        await using var comando = new MySqlCommand(consultaSql, conexion);

        comando.Parameters.AddWithValue("@Dni", propietario.Dni);
        comando.Parameters.AddWithValue("@Nombre", propietario.Nombre);
        comando.Parameters.AddWithValue("@Apellido", propietario.Apellido);
        comando.Parameters.AddWithValue("@Telefono", propietario.Telefono);
        comando.Parameters.AddWithValue("@Email", propietario.Email);
        comando.Parameters.AddWithValue("@Direccion", propietario.Direccion);

        try
        {
            var nuevoId = await comando.ExecuteScalarAsync();

            if (nuevoId == null)
                return null;

            propietario.Id = Convert.ToInt32(nuevoId);
            return propietario;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            // Error 1062: Duplicate entry (ej: DNI duplicado)
            throw new InvalidOperationException($"Ya existe un propietario con el DNI '{propietario.Dni}'.", ex);
        }
    }

    public async Task<bool> Actualizar(Propietario propietario)
    {
      await using var conexion = CrearConexion();
      await conexion.OpenAsync();

        const string consultaSql = """ 
        UPDATE propietarios
        SET dni = @Dni, nombre = @Nombre, apellido = @Apellido, telefono = @Telefono,
        email = @Email, direccion = @Direccion
        WHERE id = @Id;
        """;

        await using var comando = new MySqlCommand(consultaSql, conexion);
        comando.Parameters.AddWithValue("@Id", propietario.Id);
        comando.Parameters.AddWithValue("@Dni", propietario.Dni);
        comando.Parameters.AddWithValue("@Nombre", propietario.Nombre);
        comando.Parameters.AddWithValue("@Apellido", propietario.Apellido);
        comando.Parameters.AddWithValue("@Telefono", propietario.Telefono);
        comando.Parameters.AddWithValue("@Email", propietario.Email);
        comando.Parameters.AddWithValue("@Direccion", propietario.Direccion);

        var afectados = await comando.ExecuteNonQueryAsync();
        return afectados > 0;
    }

    public async Task<bool> Eliminar(int id)
    {
        await using var conexion = CrearConexion();
        await conexion.OpenAsync();

        const string consultaSql = """ 
        UPDATE propietarios
        SET estado = false
        WHERE id = @Id;
        """;

        await using var comando = new MySqlCommand(consultaSql, conexion);
        comando.Parameters.AddWithValue("@Id", id);

        var afectados = await comando.ExecuteNonQueryAsync();
        return afectados > 0;
    }

}