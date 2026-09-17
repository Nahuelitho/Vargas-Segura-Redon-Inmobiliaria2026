using System.Net;
using System.Text.RegularExpressions;
using Inmobiliaria.Controllers;
using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Inmobiliaria.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Xunit;

namespace Inmobiliaria.Tests;

public class EdicionPaginacionTests
{
    [MySqlFact]
    public async Task FormulariosGuardanYPaginacionRespetaSeisRegistros()
    {
        var configuracion = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
        var cadena = new MySqlConnectionStringBuilder(configuracion.GetConnectionString("DefaultConnection")!);
        var nombre = "inmobiliaria_test_" + Guid.NewGuid().ToString("N");
        cadena.Database = "";
        await using var servidor = new MySqlConnection(cadena.ConnectionString);
        await servidor.OpenAsync();
        await using (var crear = new MySqlCommand($"CREATE DATABASE `{nombre}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci", servidor))
            await crear.ExecuteNonQueryAsync();
        try
        {
            cadena.Database = nombre;
            await using var conexion = new MySqlConnection(cadena.ConnectionString);
            await conexion.OpenAsync();
            var esquema = await File.ReadAllTextAsync("database.sql");
            esquema = Regex.Replace(esquema, @"\ACREATE DATABASE.*?USE\s+\w+;", "", RegexOptions.Singleline);
            Assert.DoesNotContain("CREATE DATABASE", esquema);
            Assert.DoesNotContain("USE Lab2", esquema);
            await using (var comando = new MySqlCommand(esquema, conexion)) await comando.ExecuteNonQueryAsync();
            await using (var comando = new MySqlCommand("DELETE FROM tipos_inmueble", conexion)) await comando.ExecuteNonQueryAsync();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development", ApplicationName = typeof(CuentaController).Assembly.GetName().Name });
            builder.Logging.ClearProviders();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = cadena.ConnectionString });
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddControllersWithViews().AddApplicationPart(typeof(CuentaController).Assembly);
            builder.Services.AgregarSeguridad(true);
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
            builder.Services.AddSingleton<IUsuarioRepository>(new UsuariosMemoria());
            builder.Services.AddScoped<PropietarioRepository>(); builder.Services.AddScoped<InquilinoRepository>();
            builder.Services.AddScoped<InmuebleRepository>(); builder.Services.AddScoped<TipoInmuebleRepository>(); builder.Services.AddScoped<ReservaRepository>();
            await using var app = builder.Build();
            app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
            app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            await app.StartAsync();
            using var cliente = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() })
            { BaseAddress = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };

            async Task<string> Pagina(string ruta)
            {
                var respuesta = await cliente.GetAsync(ruta);
                Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
                return await respuesta.Content.ReadAsStringAsync();
            }
            async Task<HttpResponseMessage> Guardar(string ruta, Dictionary<string, string> campos)
            {
                var html = await Pagina(ruta);
                campos["__RequestVerificationToken"] = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
                return await cliente.PostAsync(ruta, new FormUrlEncodedContent(campos));
            }
            Assert.Equal(HttpStatusCode.Redirect, (await Guardar("/Cuenta/Login", new() { ["Email"] = "administrador@example.test", ["Password"] = UsuariosMemoria.Clave })).StatusCode);
            var entidades = new[] { "Propietario", "Inquilino", "Inmueble", "TipoInmueble", "Reserva" };
            async Task VerificarPaginas(int total)
            {
                foreach (var entidad in entidades)
                {
                    var html = await Pagina($"/{entidad}");
                    Assert.Equal(total > 6, html.Contains("aria-label=\"Paginación\""));
                    Assert.Equal(Math.Min(total, 6), Regex.Matches(html, $"href=\"/{entidad}/Editar/").Count);
                    if (total > 6)
                    {
                        var ultima = await Pagina($"/{entidad}?pagina=99");
                        Assert.Equal(total - 6, Regex.Matches(ultima, $"href=\"/{entidad}/Editar/").Count);
                        Assert.DoesNotContain("pagina=3", ultima);
                        Assert.Contains("pagina=1", ultima);
                        var idsPrimera = Regex.Matches(html, $"href=\"/{entidad}/Editar/([0-9]+)").Select(m => m.Groups[1].Value);
                        var idsUltima = Regex.Matches(ultima, $"href=\"/{entidad}/Editar/([0-9]+)").Select(m => m.Groups[1].Value);
                        Assert.Empty(idsPrimera.Intersect(idsUltima));
                    }
                }
            }
            await VerificarPaginas(0);
            var propietarios = new PropietarioRepository(builder.Configuration);
            var inquilinos = new InquilinoRepository(builder.Configuration);
            var inmuebles = new InmuebleRepository(builder.Configuration);
            var tipos = new TipoInmuebleRepository(builder.Configuration);
            var reservas = new ReservaRepository(builder.Configuration);
            int idInmueble = 0, idTipo = 0, idPropietario = 0, idReserva = 0;
            for (var i = 1; i <= 12; i++)
            {
                idPropietario = (await propietarios.Crear(new Propietario { Dni = $"{i}", Nombre = "Nombre", Apellido = $"{i:00}", Telefono = "123", Email = "a@example.test", Direccion = "Direccion" }))!.Id;
                var inquilino = await inquilinos.Crear(new Inquilino { Dni = $"{i}", Nombre = "Nombre", Apellido = $"{i:00}", Telefono = "123", Email = "b@example.test", Direccion = "Direccion" });
                idTipo = await tipos.Crear(new TipoInmueble { Descripcion = $"Tipo {i:00}" });
                idInmueble = await inmuebles.Crear(new Inmueble { IdPropietario = idPropietario, IdTipo = idTipo, Direccion = $"Calle {i:00}", Cupo = 2, Coordenadas = "-33,-66", PrecioPorDia = 100, PorcentajeReserva = 20 });
                idReserva = await reservas.Crear(new Reserva { IdInquilino = inquilino!.Id, IdInmueble = idInmueble, FechaInicio = new DateTime(2026, 1, i), FechaFin = new DateTime(2026, 1, i + 1), MontoPorDia = 100 });
                if (i is 6 or 7 or 12) await VerificarPaginas(i);
            }
            Assert.Contains("FechaFin", await Pagina($"/Reserva/Editar/{idReserva}"));
            Assert.Equal(HttpStatusCode.Redirect, (await Guardar($"/TipoInmueble/Editar/{idTipo}", new() { ["Id"] = idTipo.ToString(), ["Descripcion"] = "Tipo editado" })).StatusCode);
            Assert.Equal("Tipo editado", (await tipos.ObtenerPorId(idTipo))!.Descripcion);
            var duplicado = await Guardar($"/TipoInmueble/Editar/{idTipo}", new() { ["Id"] = idTipo.ToString(), ["Descripcion"] = "Tipo 01" });
            Assert.Equal(HttpStatusCode.OK, duplicado.StatusCode);
            Assert.Contains("Ya existe", await duplicado.Content.ReadAsStringAsync());

            var vista = await Pagina($"/Inmueble/Editar/{idInmueble}");
            Assert.Matches($"<option[^>]*selected=\"selected\"[^>]*value=\"{idPropietario}\"", vista);
            var campos = new Dictionary<string, string> { ["Id"] = idInmueble.ToString(), ["IdPropietario"] = idPropietario.ToString(), ["IdTipo"] = idTipo.ToString(), ["Direccion"] = "Direccion editada", ["Cupo"] = "3", ["Coordenadas"] = "-33,-66", ["PrecioPorDia"] = "125", ["PorcentajeReserva"] = "25", ["Disponible"] = "true" };
            Assert.Equal(HttpStatusCode.Redirect, (await Guardar($"/Inmueble/Editar/{idInmueble}", campos)).StatusCode);
            Assert.Equal("Direccion editada", (await inmuebles.ObtenerPorId(idInmueble))!.Direccion);
            campos["Cupo"] = "0";
            Assert.Equal(HttpStatusCode.OK, (await Guardar($"/Inmueble/Editar/{idInmueble}", campos)).StatusCode);
            Assert.Equal(3, (await inmuebles.ObtenerPorId(idInmueble))!.Cupo);
        }
        finally
        {
            if (!Regex.IsMatch(nombre, "^inmobiliaria_test_[a-f0-9]{32}$")) throw new InvalidOperationException("Base inesperada.");
            await using var borrar = new MySqlCommand($"DROP DATABASE `{nombre}`", servidor);
            await borrar.ExecuteNonQueryAsync();
        }
    }
}
