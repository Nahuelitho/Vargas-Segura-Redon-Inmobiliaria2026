namespace Inmobiliaria.Repositories;

using Inmobiliaria.Models;
using MySqlConnector;

public class InquilinoRepository(IConfiguration config)
{

    private readonly string _cadenaConexion = config.GetConnectionString("DefaultConnection")!;

    private MySqlConnection CrearConexion()
    {
      return new MySqlConnection(_cadenaConexion);
    }

    public async Task<List<Inquilino>> ObtenerTodos(int pagina = 1, int limite = 10)
    {
        var inquilinos = new List<Inquilino>();

        await using var conexion = CrearConexion();
        await conexion.OpenAsync();

        const string consultaSql = """ 
        SELECT * FROM inquilinos WHERE estado = true
        LIMIT @limit OFFSET @offset;
        """;

        await using var comando = new MySqlCommand(consultaSql, conexion);
        comando.Parameters.AddWithValue("@limit", limite);
        comando.Parameters.AddWithValue("@offset", (pagina - 1) * limite);
        await using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            inquilinos.Add(new Inquilino
            {
                Id = lector.GetInt32("Id"),
                Dni = lector.GetString("dni"),
                Nombre = lector.GetString("nombre"),
                Apellido = lector.GetString("apellido"),
                Telefono = lector["telefono"]?.ToString() ?? "",
                Email = lector["email"]?.ToString() ?? "",
                Direccion = lector["direccion"]?.ToString() ?? "",
                Estado = lector.GetBoolean("estado")
            });
        }

        return inquilinos;
    }

    public async Task<Inquilino?> ObtenerPorId(int id)
    {
        await using var conexion = CrearConexion();
        await conexion.OpenAsync();

        const string consultaSql = """ 
          SELECT * FROM inquilinos WHERE id = @id AND estado = true;
        """;

        await using var comando = new MySqlCommand(consultaSql, conexion);
        comando.Parameters.AddWithValue("@id", id);

        await using var lector = await comando.ExecuteReaderAsync();

        if (await lector.ReadAsync()){
          return new Inquilino{
            Id = lector.GetInt32("Id"),
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

    public async Task<Inquilino?> Crear(Inquilino inquilino){
        await using var conexion = CrearConexion();
        await conexion.OpenAsync();

        const string consultaSql = """ 
          INSERT INTO inquilinos (dni, nombre, apellido, telefono, email, direccion, estado)
          VALUES (@Dni, @Nombre, @Apellido, @Telefono, @Email, @Direccion, true);
          SELECT last_insert_id();
        """;

        await using var comando = new MySqlCommand(consultaSql, conexion);
        comando.Parameters.AddWithValue("@Dni", inquilino.Dni);
        comando.Parameters.AddWithValue("@Nombre", inquilino.Nombre);
        comando.Parameters.AddWithValue("@Apellido", inquilino.Apellido);
        comando.Parameters.AddWithValue("@Telefono", inquilino.Telefono);
        comando.Parameters.AddWithValue("@Email", inquilino.Email);
        comando.Parameters.AddWithValue("@Direccion", inquilino.Direccion);

        try
        {
            var nuevoId = await comando.ExecuteScalarAsync();

            if(nuevoId == null){
              return null;
            }

            inquilino.Id = Convert.ToInt32(nuevoId);
            return inquilino;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new InvalidOperationException($"Ya existe un inquilino con el DNI '{inquilino.Dni}'.", ex);
        }
    }

    public async Task<bool> Actualizar(Inquilino inquilino)
    {
      await using var conexion = CrearConexion();
      await conexion.OpenAsync();

        const string consultaSql = """ 
        UPDATE inquilinos
        SET dni = @Dni, nombre = @Nombre, apellido = @Apellido, telefono = @Telefono,
        email = @Email, direccion = @Direccion
        WHERE id = @Id;
        """;

        await using var comando = new MySqlCommand(consultaSql, conexion);
        comando.Parameters.AddWithValue("@Id", inquilino.Id);
        comando.Parameters.AddWithValue("@Dni", inquilino.Dni);
        comando.Parameters.AddWithValue("@Nombre", inquilino.Nombre);
        comando.Parameters.AddWithValue("@Apellido", inquilino.Apellido);
        comando.Parameters.AddWithValue("@Telefono", inquilino.Telefono);
        comando.Parameters.AddWithValue("@Email", inquilino.Email);
        comando.Parameters.AddWithValue("@Direccion", inquilino.Direccion);

        var afectados = await comando.ExecuteNonQueryAsync();
        return afectados > 0;
    }

    public async Task<bool> Eliminar(int id)
    {
        await using var conexion = CrearConexion();
        await conexion.OpenAsync();

        const string consultaSql = """ 
        UPDATE inquilinos
        SET estado = false
        WHERE id = @Id;
        """;

        await using var comando = new MySqlCommand(consultaSql, conexion);
        comando.Parameters.AddWithValue("@Id", id);

        var afectados = await comando.ExecuteNonQueryAsync();
        return afectados > 0;
    }
}