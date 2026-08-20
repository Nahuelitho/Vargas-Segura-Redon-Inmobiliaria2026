namespace Inmobiliaria.Repositories;

using Inmobiliaria.Models;
using MySqlConnector;

public class InquilinoRepository(IConfiguration config)
{

    private readonly string _connectionString = config.GetConnectionString("DefaultConnection")!;

    private MySqlConnection CreateConnection()
    {
      return new MySqlConnection(_connectionString);
    }

    public async Task<List<Inquilino>> ObtenerTodos()
    {
        var inquilinos = new List<Inquilino>();

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sql = """ 
        SELECT * FROM inquilinos WHERE estado = true;
        """;

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            inquilinos.Add(new Inquilino
            {
                Id = reader.GetInt32("Id"),
                Dni = reader.GetString("dni"),
                Nombre = reader.GetString("nombre"),
                Apellido = reader.GetString("apellido"),
                Telefono = reader["telefono"] as string,
                Email = reader["email"] as string,
                Direccion = reader["direccion"] as string,
                Estado = reader.GetBoolean("estado")
            });
        }

        return inquilinos;
    }

    public async Task<Inquilino?> ObtenerPorId(int id)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sql = """ 
          SELECT * FROM inquilinos WHERE id = @id AND estado = true;
        """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync()){
          return new Inquilino{
            Id = reader.GetInt32("Id"),
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

    public async Task<Inquilino?> Create(Inquilino inquilino){
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sql = """ 
          INSERT INTO inquilinos (dni, nombre, apellido, telefono, email, direccion, estado)
          VALUES (@Dni, @Nombre, @Apellido, @Telefono, @Email, @Direccion, true);
          SELECT last_insert_id();
        """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Dni", inquilino.Dni);
        command.Parameters.AddWithValue("@Nombre", inquilino.Nombre);
        command.Parameters.AddWithValue("@Apellido", inquilino.Apellido);
        command.Parameters.AddWithValue("@Telefono", inquilino.Telefono);
        command.Parameters.AddWithValue("@Email", inquilino.Email);
        command.Parameters.AddWithValue("@Direccion", inquilino.Direccion);

        try
        {
            var newId = await command.ExecuteScalarAsync();

            if(newId == null){
              return null;
            }

            inquilino.Id = Convert.ToInt32(newId);
            return inquilino;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new InvalidOperationException($"Ya existe un inquilino con el DNI '{inquilino.Dni}'.", ex);
        }
    }

    public async Task<bool> Update(Inquilino inquilino)
    {
      await using var connection = CreateConnection();
      await connection.OpenAsync();

        const string sql = """ 
        UPDATE inquilinos
        SET dni = @Dni, nombre = @Nombre, apellido = @Apellido, telefono = @Telefono,
        email = @Email, direccion = @Direccion
        WHERE id = @Id;
        """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", inquilino.Id);
        command.Parameters.AddWithValue("@Dni", inquilino.Dni);
        command.Parameters.AddWithValue("@Nombre", inquilino.Nombre);
        command.Parameters.AddWithValue("@Apellido", inquilino.Apellido);
        command.Parameters.AddWithValue("@Telefono", inquilino.Telefono);
        command.Parameters.AddWithValue("@Email", inquilino.Email);
        command.Parameters.AddWithValue("@Direccion", inquilino.Direccion);

        var affected = await command.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<bool> Delete(int id)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sql = """ 
        UPDATE inquilinos
        SET estado = false
        WHERE id = @Id;
        """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        var affected = await command.ExecuteNonQueryAsync();
        return affected > 0;
    }
}