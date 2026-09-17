using System.Security.Claims;
using System.Threading.RateLimiting;
using Inmobiliaria.Models;
using Inmobiliaria.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Inmobiliaria.Services;

public static class Seguridad
{
    public const string SelloClaim = "sello_seguridad";

    public static IServiceCollection AgregarSeguridad(this IServiceCollection services, bool desarrollo)
    {
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddScoped<AvatarService>();
        services.Configure<MvcOptions>(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.LoginPath = "/Cuenta/Login";
            options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
            options.Cookie.Name = "Inmobiliaria.Sesion";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = desarrollo ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events.OnValidatePrincipal = async context =>
            {
                var usuarios = context.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();
                var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var usuario = int.TryParse(id, out var numero) ? await usuarios.ObtenerPorId(numero) : null;
                if (usuario is null || !usuario.Estado || usuario.SelloSeguridad != context.Principal?.FindFirstValue(SelloClaim))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync();
                }
            };
        });
        services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "local",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        return services;
    }

    public static Task IniciarSesion(HttpContext contexto, Usuario usuario) => contexto.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido}"),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol),
            new Claim(SelloClaim, usuario.SelloSeguridad)
        }, CookieAuthenticationDefaults.AuthenticationScheme)),
        new AuthenticationProperties { IsPersistent = false });

    public static int IdUsuario(ClaimsPrincipal usuario) => int.Parse(usuario.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
