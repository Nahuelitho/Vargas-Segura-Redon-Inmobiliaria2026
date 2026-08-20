using System.Data;
using Inmobiliaria.Models;
using MySqlConnector;

namespace Inmobiliaria.Repositories;

public class PropietarioRepository(IConfiguration config)
{
  private readonly string _connectionString = config.GetConnectionString("DefaultConnection")!;

  private MySqlConnection CreateConnection()
  {
    return new MySqlConnection(_connectionString);
  }

  // LISTAR
  public async Task<List<Propietario>> ObtenerTodos(int paginaActual = 1, int limite = 10)
  {
        var propietarios = new List<Propietario>();

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sql = """
        SELECT Id, Dni, Nombre, Apellido, Telefono, Email, Direccion, Estado
        FROM Propietarios
        WHERE Estado = true
        LIMIT @limit OFFSET @offset
        """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@limit", limite);
        command.Parameters.AddWithValue("@offset", (paginaActual - 1) * limite);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            propietarios.Add(new Propietario
            {
                Id       = reader.GetInt32("id"),
                Dni      = reader.GetString("dni"),
                Nombre   = reader.GetString("nombre"),
                Apellido = reader.GetString("apellido"),
                Telefono = reader["telefono"] as string,
                Email    = reader["email"] as string,
                Direccion= reader["direccion"] as string,
                Estado   = reader.GetBoolean("estado")
            });
        }

        return propietarios;
    }

    // Obtener un propietario por id
    public async Task<Propietario?> ObtenerPorId(int id)
    {
      await using var connection = CreateConnection();
      await connection.OpenAsync();

      const string sql = """
        SELECT * FROM propietarios WHERE id = @Id AND estado = true;
      """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync();

        if(await reader.ReadAsync()){
            return new Propietario{
                Id = reader.GetInt32("id"),
                Dni = reader.GetString("dni"),
                Nombre = reader.GetString("nombre"),
                Apellido = reader.GetString("apellido"),
                Telefono = reader["telefono"] as string,
                Email = reader["email"] as string,
                Direccion = reader["direccion"] as string,
                Estado = reader.GetBoolean("estado")
            };
        }
        return null;
    }

    //CREAR
    public async Task<Propietario?> Create(Propietario propietario)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sql = """ 
          INSERT INTO propietarios (dni, nombre, apellido, telefono, email, direccion, estado)
          VALUES (@Dni, @Nombre, @Apellido, @Telefono, @Email, @Direccion, 1);
          SELECT last_insert_id();
         """;

        await using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Dni", propietario.Dni);
        command.Parameters.AddWithValue("@Nombre", propietario.Nombre);
        command.Parameters.AddWithValue("@Apellido", propietario.Apellido);
        command.Parameters.AddWithValue("@Telefono", propietario.Telefono);
        command.Parameters.AddWithValue("@Email", propietario.Email);
        command.Parameters.AddWithValue("@Direccion", propietario.Direccion);

        try
        {
            var newId = await command.ExecuteScalarAsync();

            if (newId == null)
                return null;

            propietario.Id = Convert.ToInt32(newId);
            return propietario;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            // Error 1062: Duplicate entry (ej: DNI duplicado)
            throw new InvalidOperationException($"Ya existe un propietario con el DNI '{propietario.Dni}'.", ex);
        }
    }

    public async Task<bool> Update(Propietario propietario)
    {
      await using var connection = CreateConnection();
      await connection.OpenAsync();

        const string sql = """ 
        UPDATE propietarios
        SET dni = @Dni, nombre = @Nombre, apellido = @Apellido, telefono = @Telefono,
        email = @Email, direccion = @Direccion
        WHERE id = @Id;
        """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", propietario.Id);
        command.Parameters.AddWithValue("@Dni", propietario.Dni);
        command.Parameters.AddWithValue("@Nombre", propietario.Nombre);
        command.Parameters.AddWithValue("@Apellido", propietario.Apellido);
        command.Parameters.AddWithValue("@Telefono", propietario.Telefono);
        command.Parameters.AddWithValue("@Email", propietario.Email);
        command.Parameters.AddWithValue("@Direccion", propietario.Direccion);

        var affected = await command.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<bool> Delete(int id)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sql = """ 
        UPDATE propietarios
        SET estado = false
        WHERE id = @Id;
        """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        var affected = await command.ExecuteNonQueryAsync();
        return affected > 0;
    }

}