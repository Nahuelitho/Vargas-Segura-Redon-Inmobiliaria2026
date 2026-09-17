using System.Text.RegularExpressions;
using System.Diagnostics;
using Inmobiliaria.Controllers;
using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Xunit;

namespace Inmobiliaria.Tests;

public class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("INMOBILIARIA_TEST_MYSQL") != "1")
            Skip = "Optativa: requiere MySQL local y permiso para crear una base temporal de prueba.";
    }
}

public class UsuarioRepositoryTests
{
    [MySqlFact]
    public async Task PersistenciaMigracionPermisosYProteccionDelUltimoAdministrador()
    {
        var configuracion = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
        var cadena = new MySqlConnectionStringBuilder(configuracion.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Falta configurar la conexión de MySQL."));
        // Nombre exclusivo generado por esta prueba. Nunca se usa ni elimina la base de la aplicación.
        var nombre = "inmobiliaria_test_" + Guid.NewGuid().ToString("N");
        cadena.Database = "";
        await using var servidor = new MySqlConnection(cadena.ConnectionString);
        await servidor.OpenAsync();
        await using (var crear = new MySqlCommand($"CREATE DATABASE `{nombre}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci", servidor))
            await crear.ExecuteNonQueryAsync();
        try
        {
            cadena.Database = nombre;
            var opciones = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = cadena.ConnectionString }).Build();
            var repositorio = new UsuarioRepository(opciones);
            await repositorio.InicializarTabla();
            await repositorio.InicializarTabla(); // La migración se puede repetir sin borrar datos.
            var hasher = new PasswordHasher<Usuario>();
            Usuario Nuevo(string email, string rol) => new() { Nombre = "Prueba", Apellido = "Temporal", Email = email, Rol = rol, PasswordHash = hasher.HashPassword(new Usuario(), UsuariosMemoria.Clave) };
            // Ejecuta el comando real de instalación contra la base temporal.
            var inicio = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
            };
            inicio.ArgumentList.Add(typeof(CuentaController).Assembly.Location);
            inicio.ArgumentList.Add("--crear-admin");
            inicio.Environment["ConnectionStrings__DefaultConnection"] = cadena.ConnectionString;
            using (var proceso = Process.Start(inicio)!)
            {
                var salida = proceso.StandardOutput.ReadToEndAsync();
                var error = proceso.StandardError.ReadToEndAsync();
                await proceso.StandardInput.WriteLineAsync($"Prueba\nTemporal\n ADMIN@example.test \n{UsuariosMemoria.Clave}\n{UsuariosMemoria.Clave}");
                proceso.StandardInput.Close();
                using var tiempo = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                try { await proceso.WaitForExitAsync(tiempo.Token); }
                catch (OperationCanceledException) { proceso.Kill(entireProcessTree: true); throw; }
                Assert.True(proceso.ExitCode == 0, await error);
                Assert.Contains("Administrador creado", await salida);
            }
            var admin = await repositorio.ObtenerPorEmail("ADMIN@example.test");
            Assert.NotNull(admin);
            var idAdmin = admin.Id;
            Assert.Equal("admin@example.test", admin.Email);
            await Assert.ThrowsAsync<InvalidOperationException>(() => repositorio.Crear(Nuevo("otro@example.test", Roles.Administrador), true));
            await Assert.ThrowsAsync<InvalidOperationException>(() => repositorio.Eliminar(idAdmin));
            admin.Rol = Roles.Empleado;
            await Assert.ThrowsAsync<InvalidOperationException>(() => repositorio.Actualizar(admin));
            Assert.Equal(Roles.Administrador, (await repositorio.ObtenerPorId(idAdmin))!.Rol);

            var id = await repositorio.Crear(Nuevo("empleado@example.test", Roles.Empleado));
            await Assert.ThrowsAsync<InvalidOperationException>(() => repositorio.Crear(Nuevo("EMPLEADO@example.test", Roles.Empleado)));
            var empleado = (await repositorio.ObtenerPorId(id))!;
            var sello = empleado.SelloSeguridad;
            await repositorio.ActualizarPerfil(id, new PerfilFormulario { Nombre = "Editado", Apellido = "Perfil", Email = "perfil@example.test" }, "foto.png");
            empleado = (await repositorio.ObtenerPorId(id))!;
            Assert.Equal("foto.png", empleado.Avatar);
            Assert.Equal(Roles.Empleado, empleado.Rol);
            Assert.NotEqual(sello, empleado.SelloSeguridad);
            await Assert.ThrowsAsync<InvalidOperationException>(() => repositorio.ActualizarPerfil(id, new PerfilFormulario { Nombre = "Editado", Apellido = "Perfil", Email = "admin@example.test" }, null));
            await Assert.ThrowsAsync<InvalidOperationException>(() => repositorio.CambiarPassword(id, "hash", sello));
            var hash = hasher.HashPassword(empleado, "Otra.Clave.Segura");
            await repositorio.CambiarPassword(id, hash, empleado.SelloSeguridad);
            empleado = (await repositorio.ObtenerPorId(id))!;
            Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(empleado, empleado.PasswordHash, "Otra.Clave.Segura"));
            empleado.Rol = Roles.Administrador;
            await repositorio.Actualizar(empleado);
            await repositorio.Eliminar(idAdmin);
            Assert.Null(await repositorio.ObtenerPorId(idAdmin));
            Assert.Null(await repositorio.ObtenerPorEmail("admin@example.test"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => repositorio.Eliminar(id));
            var listado = await repositorio.Listar(999, "perfil");
            Assert.Single(listado.Usuarios);
            Assert.Equal(1, listado.Pagina);
            Assert.Empty((await repositorio.Listar(1, "%")).Usuarios);
            for (var i = 0; i < 11; i++) await repositorio.Crear(Nuevo($"lista{i}@example.test", Roles.Empleado));
            var segundaPagina = await repositorio.Listar(2, null);
            Assert.Equal(2, segundaPagina.TotalPaginas);
            Assert.Equal(2, segundaPagina.Usuarios.Count);
        }
        finally
        {
            // Solo se elimina la base exclusiva que acaba de crear esta prueba.
            if (!Regex.IsMatch(nombre, "^inmobiliaria_test_[a-f0-9]{32}$")) throw new InvalidOperationException("Nombre de base de prueba inesperado.");
            MySqlConnection.ClearAllPools();
            await using var borrar = new MySqlCommand($"DROP DATABASE `{nombre}`", servidor);
            await borrar.ExecuteNonQueryAsync();
        }
    }
}
