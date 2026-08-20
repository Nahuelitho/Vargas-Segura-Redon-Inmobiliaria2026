namespace Inmobiliaria.Repositories;

using Inmobiliaria.Models;
using MySqlConnector;

public class InquilinoRepository(IConfiguration config)
{

    private readonly string _connectionString = config.GetConnectionString("DefaultConnection")!;

    private MySqlConnection CreateConection()
    {
      return new MySqlConnection(_connectionString);
    }

    public async Task<List<Inquilino>> ObtenerTodos()
    {
        var inquilinos = new List<Inquilino>();

        await using var connection = CreateConection();
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
        await using var connection = CreateConection();
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
}