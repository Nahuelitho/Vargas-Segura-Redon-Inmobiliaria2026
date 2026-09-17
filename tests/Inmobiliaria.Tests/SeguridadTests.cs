using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Inmobiliaria.Controllers;
using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Inmobiliaria.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Inmobiliaria.Tests;

public class SeguridadTests : IAsyncLifetime
{
    private WebApplication app = null!;
    private Uri direccion = null!;
    private readonly UsuariosMemoria usuarios = new();

    public async Task InitializeAsync()
    {
        var raiz = Path.Combine(AppContext.BaseDirectory, "App_Data", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(raiz);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development", ApplicationName = typeof(CuentaController).Assembly.GetName().Name, ContentRootPath = raiz });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(CuentaController).Assembly);
        builder.Services.AgregarSeguridad(true);
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddSingleton<IUsuarioRepository>(usuarios);
        app = builder.Build();
        app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        await app.StartAsync();
        direccion = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
    }

    public async Task DisposeAsync() => await app.DisposeAsync();
    private HttpClient Cliente() => new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = direccion };
    private static async Task<string> Token(HttpClient cliente, string ruta)
    {
        var respuesta = await cliente.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await respuesta.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, html);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    private static async Task<HttpResponseMessage> Enviar(HttpClient cliente, string ruta, Dictionary<string, string> valores, string? formulario = null)
    {
        valores["__RequestVerificationToken"] = await Token(cliente, formulario ?? ruta);
        return await cliente.PostAsync(ruta, new FormUrlEncodedContent(valores));
    }
    private static async Task Ingresar(HttpClient cliente, string email = "empleado@example.test")
    {
        var respuesta = await Enviar(cliente, "/Cuenta/Login", new() { ["Email"] = email, ["Password"] = UsuariosMemoria.Clave });
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Propietario")]
    [InlineData("/Inquilino")]
    [InlineData("/Inmueble")]
    [InlineData("/TipoInmueble")]
    [InlineData("/Reserva")]
    [InlineData("/Usuario")]
    [InlineData("/Cuenta/Perfil")]
    public async Task AnonimoDebeIniciarSesion(string ruta)
    {
        using var cliente = Cliente();
        var respuesta = await cliente.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Cuenta/Login", respuesta.Headers.Location!.ToString());
    }

    [Fact]
    public async Task LoginValidaClaveYEvitaRedireccionExterna()
    {
        using var cliente = Cliente();
        var error = await Enviar(cliente, "/Cuenta/Login", new() { ["Email"] = "empleado@example.test", ["Password"] = "incorrecta" });
        Assert.Contains("incorrectos", await error.Content.ReadAsStringAsync());
        var correcto = await Enviar(cliente, "/Cuenta/Login", new() { ["Email"] = " EMPLEADO@example.test ", ["Password"] = UsuariosMemoria.Clave, ["ReturnUrl"] = "https://example.org/externo" });
        Assert.Equal(HttpStatusCode.Redirect, correcto.StatusCode);
        Assert.DoesNotContain("example.org", correcto.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/Cuenta/Perfil")).StatusCode);
    }

    [Theory]
    [InlineData("/Usuario/Crear", false)]
    [InlineData("/Usuario/Editar/1", false)]
    [InlineData("/Usuario/Eliminar/1", true)]
    [InlineData("/Propietario/Eliminar/1", true)]
    [InlineData("/Inquilino/Eliminar/1", true)]
    [InlineData("/Inmueble/Eliminar/1", true)]
    [InlineData("/TipoInmueble/Eliminar/1", true)]
    [InlineData("/Reserva/Eliminar/1", true)]
    public async Task EmpleadoNoPuedeGestionarUsuariosNiEliminar(string ruta, bool post)
    {
        using var cliente = Cliente(); await Ingresar(cliente);
        var respuesta = post ? await cliente.PostAsync(ruta, new FormUrlEncodedContent(new Dictionary<string, string>())) : await cliente.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("AccesoDenegado", respuesta.Headers.Location!.ToString());
    }

    [Fact]
    public async Task PerfilIgnoraIdRolYEstadoManipulados()
    {
        using var cliente = Cliente(); await Ingresar(cliente);
        var respuesta = await Enviar(cliente, "/Cuenta/Perfil", new() { ["Id"] = "1", ["Rol"] = "Administrador", ["Estado"] = "false", ["Nombre"] = "Nuevo", ["Apellido"] = "Empleado", ["Email"] = "nuevo@example.test" });
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("Nuevo", usuarios.Datos[1].Nombre);
        Assert.Equal(Roles.Empleado, usuarios.Datos[1].Rol);
        Assert.True(usuarios.Datos[1].Estado);
        Assert.Equal("Administrador", usuarios.Datos[0].Nombre);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/Cuenta/Perfil")).StatusCode);
    }

    [Fact]
    public async Task AccionesPostExigenAntifalsificacion()
    {
        using var cliente = Cliente();
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsync("/Cuenta/Login", new FormUrlEncodedContent(new Dictionary<string, string> { ["Email"] = "empleado@example.test", ["Password"] = UsuariosMemoria.Clave }))).StatusCode);
        await Ingresar(cliente);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsync("/Cuenta/Perfil", new StringContent(""))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsync("/Cuenta/Salir", new StringContent(""))).StatusCode);
    }

    [Fact]
    public async Task CambiarClaveExigeActualYCierraOtrasSesiones()
    {
        using var cliente = Cliente(); using var otraSesion = Cliente();
        await Ingresar(cliente); await Ingresar(otraSesion);
        var campos = new Dictionary<string, string> { ["PasswordActual"] = "incorrecta", ["PasswordNueva"] = "Otra.Clave.2026", ["Confirmacion"] = "Otra.Clave.2026" };
        var error = await Enviar(cliente, "/Cuenta/Password", campos);
        Assert.Equal(HttpStatusCode.OK, error.StatusCode);
        Assert.Contains("incorrecta", await error.Content.ReadAsStringAsync());
        campos["PasswordActual"] = UsuariosMemoria.Clave;
        Assert.Equal(HttpStatusCode.Redirect, (await Enviar(cliente, "/Cuenta/Password", campos)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await otraSesion.GetAsync("/Cuenta/Perfil")).StatusCode);
        var loginViejo = await Enviar(cliente, "/Cuenta/Login", new() { ["Email"] = "empleado@example.test", ["Password"] = UsuariosMemoria.Clave });
        Assert.Equal(HttpStatusCode.OK, loginViejo.StatusCode);
        var loginNuevo = await Enviar(cliente, "/Cuenta/Login", new() { ["Email"] = "empleado@example.test", ["Password"] = "Otra.Clave.2026" });
        Assert.Equal(HttpStatusCode.Redirect, loginNuevo.StatusCode);
    }

    [Fact]
    public async Task AdministradorCreaEditaYDaDeBajaUsuarios()
    {
        using var administrador = Cliente(); using var empleado = Cliente();
        await Ingresar(administrador, "administrador@example.test"); await Ingresar(empleado);
        Assert.Equal(HttpStatusCode.OK, (await administrador.GetAsync("/Usuario")).StatusCode);
        var crear = await Enviar(administrador, "/Usuario/Crear", new() { ["Nombre"] = "Nuevo", ["Apellido"] = "Usuario", ["Email"] = "tercero@example.test", ["Rol"] = Roles.Empleado, ["Password"] = UsuariosMemoria.Clave, ["Confirmacion"] = UsuariosMemoria.Clave });
        Assert.Equal(HttpStatusCode.Redirect, crear.StatusCode);
        Assert.NotEqual(UsuariosMemoria.Clave, usuarios.Datos[2].PasswordHash);
        var editar = await Enviar(administrador, "/Usuario/Editar/2", new() { ["Id"] = "2", ["Nombre"] = "Empleado", ["Apellido"] = "Prueba", ["Email"] = "empleado@example.test", ["Rol"] = Roles.Administrador });
        Assert.Equal(HttpStatusCode.Redirect, editar.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await empleado.GetAsync("/Cuenta/Perfil")).StatusCode);
        var baja = await Enviar(administrador, "/Usuario/Eliminar/3", new(), "/Usuario");
        Assert.Equal(HttpStatusCode.Redirect, baja.StatusCode);
        Assert.False(usuarios.Datos[2].Estado);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(administrador, "/Usuario/Eliminar/1", new(), "/Usuario")).StatusCode);
    }

    [Fact]
    public async Task BajaRevocaSesionYSalirCierraAcceso()
    {
        using var cliente = Cliente(); await Ingresar(cliente);
        usuarios.Datos[1].Estado = false;
        Assert.Equal(HttpStatusCode.Redirect, (await cliente.GetAsync("/Cuenta/Perfil")).StatusCode);
        usuarios.Datos[1].Estado = true;
        await Ingresar(cliente);
        Assert.Equal(HttpStatusCode.Redirect, (await Enviar(cliente, "/Cuenta/Salir", new(), "/Cuenta/Perfil")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await cliente.GetAsync("/Cuenta/Perfil")).StatusCode);
    }

    [Fact]
    public async Task AvatarAceptaPngRechazaSvgYPuedeQuitarse()
    {
        using var cliente = Cliente(); await Ingresar(cliente);
        async Task<HttpResponseMessage> Subir(byte[] datos, string nombre)
        {
            using var formulario = new MultipartFormDataContent();
            formulario.Add(new StringContent(await Token(cliente, "/Cuenta/Perfil")), "__RequestVerificationToken");
            formulario.Add(new StringContent("Empleado"), "Nombre"); formulario.Add(new StringContent("Prueba"), "Apellido"); formulario.Add(new StringContent("empleado@example.test"), "Email");
            formulario.Add(new ByteArrayContent(datos), "ArchivoAvatar", nombre);
            return await cliente.PostAsync("/Cuenta/Perfil", formulario);
        }
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aU1sAAAAASUVORK5CYII=");
        Assert.Equal(HttpStatusCode.Redirect, (await Subir(png, "../../foto.png")).StatusCode);
        var avatar = await cliente.GetAsync("/Cuenta/Avatar");
        Assert.Equal("image/png", avatar.Content.Headers.ContentType!.MediaType);
        Assert.Equal(png, await avatar.Content.ReadAsByteArrayAsync());
        var anterior = usuarios.Datos[1].Avatar;
        Assert.Equal(HttpStatusCode.OK, (await Subir(Encoding.UTF8.GetBytes("<svg onload='alert(1)'></svg>"), "foto.png")).StatusCode);
        Assert.Equal(anterior, usuarios.Datos[1].Avatar);
        Assert.Equal(HttpStatusCode.OK, (await Subir(new byte[AvatarService.MaximoBytes + 1], "grande.png")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await Enviar(cliente, "/Cuenta/Perfil", new() { ["Nombre"] = "Empleado", ["Apellido"] = "Prueba", ["Email"] = "empleado@example.test", ["QuitarAvatar"] = "true" })).StatusCode);
        Assert.Null(usuarios.Datos[1].Avatar);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/Cuenta/Avatar")).StatusCode);
    }

    [Fact]
    public async Task LoginLimitaIntentos()
    {
        using var cliente = Cliente();
        var token = await Token(cliente, "/Cuenta/Login");
        HttpResponseMessage respuesta = null!;
        for (var i = 0; i < 11; i++)
            respuesta = await cliente.PostAsync("/Cuenta/Login", new FormUrlEncodedContent(new Dictionary<string, string> { ["Email"] = "empleado@example.test", ["Password"] = "incorrecta", ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.TooManyRequests, respuesta.StatusCode);
    }
}
