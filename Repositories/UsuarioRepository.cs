using Inmobiliaria.Models;
using MySqlConnector;

namespace Inmobiliaria.Repositories;

public class UsuarioRepository(IConfiguration config) : IUsuarioRepository
{
    private MySqlConnection Conexion() => new(config.GetConnectionString("DefaultConnection"));
    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();

    public async Task InicializarTabla()
    {
        using var recurso = typeof(UsuarioRepository).Assembly.GetManifestResourceStream("Inmobiliaria.Database.001_usuarios.sql")!;
        using var lector = new StreamReader(recurso);
        await using var conexion = Conexion();
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand(await lector.ReadToEndAsync(), conexion);
        await comando.ExecuteNonQueryAsync();
    }

    public Task<Usuario?> ObtenerPorId(int id) => Obtener("id = @valor", id);
    public Task<Usuario?> ObtenerPorEmail(string email) => Obtener("email = @valor", NormalizarEmail(email));
    private async Task<Usuario?> Obtener(string condicion, object valor)
    {
        await using var conexion = Conexion();
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand($"SELECT * FROM usuarios WHERE {condicion} AND estado = true", conexion);
        comando.Parameters.AddWithValue("@valor", valor);
        await using var lector = await comando.ExecuteReaderAsync();
        return await lector.ReadAsync() ? Mapear(lector) : null;
    }

    public async Task<UsuariosListado> Listar(int pagina, string? buscar)
    {
        const int limite = 10;
        buscar = buscar?.Trim();
        if (buscar?.Length > 100) buscar = buscar[..100];
        var filtro = "%" + (buscar ?? "").Replace("!", "!!").Replace("%", "!%").Replace("_", "!_") + "%";
        const string condicion = "estado = true AND (email LIKE @filtro ESCAPE '!' OR CONCAT(nombre, ' ', apellido) LIKE @filtro ESCAPE '!')";
        await using var conexion = Conexion();
        await conexion.OpenAsync();
        await using var contar = new MySqlCommand($"SELECT COUNT(*) FROM usuarios WHERE {condicion}", conexion);
        contar.Parameters.AddWithValue("@filtro", filtro);
        var total = Convert.ToInt32(await contar.ExecuteScalarAsync());
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)limite));
        pagina = Math.Clamp(pagina, 1, paginas);
        await using var comando = new MySqlCommand($"SELECT * FROM usuarios WHERE {condicion} ORDER BY apellido, nombre, id LIMIT @limite OFFSET @offset", conexion);
        comando.Parameters.AddWithValue("@filtro", filtro);
        comando.Parameters.AddWithValue("@limite", limite);
        comando.Parameters.AddWithValue("@offset", (pagina - 1) * limite);
        await using var lector = await comando.ExecuteReaderAsync();
        var usuarios = new List<Usuario>();
        while (await lector.ReadAsync()) usuarios.Add(Mapear(lector));
        return new UsuariosListado(usuarios, pagina, paginas, buscar);
    }

    // Serializa altas/bajas/cambios de rol para conservar al menos un administrador.
    private async Task ConAdministradoresBloqueados(Func<MySqlConnection, MySqlTransaction, int, Task> operacion)
    {
        await using var conexion = Conexion();
        await conexion.OpenAsync();
        await using var transaccion = await conexion.BeginTransactionAsync();
        await using var comando = new MySqlCommand("SELECT id FROM usuarios WHERE rol = 'Administrador' AND estado = true ORDER BY id FOR UPDATE", conexion, transaccion);
        var cantidad = 0;
        await using (var lector = await comando.ExecuteReaderAsync())
            while (await lector.ReadAsync()) cantidad++;
        try
        {
            await operacion(conexion, transaccion, cantidad);
            await transaccion.CommitAsync();
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new InvalidOperationException("El email ya está registrado, incluso si el usuario fue dado de baja.");
        }
    }

    public async Task<int> Crear(Usuario usuario, bool primerAdministrador = false)
    {
        var id = 0;
        await ConAdministradoresBloqueados(async (conexion, transaccion, cantidad) =>
        {
            if (primerAdministrador && (cantidad > 0 || usuario.Rol != Roles.Administrador))
                throw new InvalidOperationException("Ya existe un administrador. Gestione los usuarios desde la aplicación.");
            await using var comando = new MySqlCommand("INSERT INTO usuarios (nombre, apellido, email, password_hash, rol, sello_seguridad) VALUES (@nombre, @apellido, @email, @hash, @rol, @sello); SELECT LAST_INSERT_ID();", conexion, transaccion);
            Parametros(comando, usuario);
            id = Convert.ToInt32(await comando.ExecuteScalarAsync());
        });
        return id;
    }

    public async Task Actualizar(Usuario usuario)
    {
        await ConAdministradoresBloqueados(async (conexion, transaccion, cantidad) =>
        {
            await VerificarUltimoAdministrador(conexion, transaccion, usuario.Id, cantidad, usuario.Rol != Roles.Administrador);
            await using var comando = new MySqlCommand("UPDATE usuarios SET nombre=@nombre, apellido=@apellido, email=@email, rol=@rol, password_hash=@hash, sello_seguridad=@sello WHERE id=@id AND estado=true AND sello_seguridad=@anterior", conexion, transaccion);
            Parametros(comando, usuario);
            comando.Parameters.AddWithValue("@id", usuario.Id);
            comando.Parameters.AddWithValue("@anterior", usuario.SelloSeguridad);
            if (await comando.ExecuteNonQueryAsync() != 1)
                throw new InvalidOperationException("El usuario cambió o fue dado de baja. Recargue la página.");
        });
    }

    public async Task ActualizarPerfil(int id, PerfilFormulario perfil, string? avatar)
    {
        await using var conexion = Conexion();
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand("UPDATE usuarios SET nombre=@nombre, apellido=@apellido, email=@email, avatar=@avatar, sello_seguridad=@sello WHERE id=@id AND estado=true", conexion);
        comando.Parameters.AddWithValue("@nombre", perfil.Nombre.Trim());
        comando.Parameters.AddWithValue("@apellido", perfil.Apellido.Trim());
        comando.Parameters.AddWithValue("@email", NormalizarEmail(perfil.Email));
        comando.Parameters.AddWithValue("@avatar", (object?)avatar ?? DBNull.Value);
        comando.Parameters.AddWithValue("@sello", Guid.NewGuid().ToString("N"));
        comando.Parameters.AddWithValue("@id", id);
        try
        {
            if (await comando.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("El usuario ya no está activo.");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new InvalidOperationException("El email ya está registrado.");
        }
    }

    public async Task CambiarPassword(int id, string hash, string selloAnterior)
    {
        await using var conexion = Conexion();
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand("UPDATE usuarios SET password_hash=@hash, sello_seguridad=@sello WHERE id=@id AND estado=true AND sello_seguridad=@anterior", conexion);
        comando.Parameters.AddWithValue("@hash", hash);
        comando.Parameters.AddWithValue("@sello", Guid.NewGuid().ToString("N"));
        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@anterior", selloAnterior);
        if (await comando.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("El usuario cambió. Inicie sesión nuevamente.");
    }

    public Task Eliminar(int id) => ConAdministradoresBloqueados(async (conexion, transaccion, cantidad) =>
    {
        await VerificarUltimoAdministrador(conexion, transaccion, id, cantidad, true);
        await using var comando = new MySqlCommand("UPDATE usuarios SET estado=false, sello_seguridad=@sello WHERE id=@id AND estado=true", conexion, transaccion);
        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@sello", Guid.NewGuid().ToString("N"));
        if (await comando.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("El usuario ya no está activo.");
    });

    private static async Task VerificarUltimoAdministrador(MySqlConnection conexion, MySqlTransaction transaccion, int id, int cantidad, bool pierdeRol)
    {
        if (!pierdeRol || cantidad > 1) return;
        await using var comando = new MySqlCommand("SELECT rol FROM usuarios WHERE id=@id AND estado=true", conexion, transaccion);
        comando.Parameters.AddWithValue("@id", id);
        if (await comando.ExecuteScalarAsync() as string == Roles.Administrador)
            throw new InvalidOperationException("Debe quedar al menos un administrador activo.");
    }

    private static void Parametros(MySqlCommand comando, Usuario usuario)
    {
        comando.Parameters.AddWithValue("@nombre", usuario.Nombre.Trim());
        comando.Parameters.AddWithValue("@apellido", usuario.Apellido.Trim());
        comando.Parameters.AddWithValue("@email", NormalizarEmail(usuario.Email));
        comando.Parameters.AddWithValue("@hash", usuario.PasswordHash);
        comando.Parameters.AddWithValue("@rol", usuario.Rol);
        comando.Parameters.AddWithValue("@sello", Guid.NewGuid().ToString("N"));
    }

    private static Usuario Mapear(MySqlDataReader lector) => new()
    {
        Id = lector.GetInt32("id"), Nombre = lector.GetString("nombre"), Apellido = lector.GetString("apellido"),
        Email = lector.GetString("email"), PasswordHash = lector.GetString("password_hash"), Rol = lector.GetString("rol"),
        Avatar = lector.IsDBNull(lector.GetOrdinal("avatar")) ? null : lector.GetString("avatar"),
        SelloSeguridad = lector.GetString("sello_seguridad"), Estado = lector.GetBoolean("estado")
    };
}
