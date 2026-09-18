using Inmobiliaria.Repositories;
using Inmobiliaria.Services;

var builder = WebApplication.CreateBuilder(args);

// Evitar el error de permisos con Windows Event Log en desarrollo
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllersWithViews();
builder.Services.AgregarSeguridad(builder.Environment.IsDevelopment());

builder.Services.AddScoped<PropietarioRepository>();
builder.Services.AddScoped<InquilinoRepository>();
builder.Services.AddScoped<InmuebleRepository>();
builder.Services.AddScoped<TipoInmuebleRepository>();
builder.Services.AddScoped<ReservaRepository>();
builder.Services.AddScoped<PagoRepository>();

var app = builder.Build();

if (args.Contains("--migrar-pagos"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<PagoRepository>().InicializarTabla();
    Console.WriteLine("Módulo de pagos preparado. Los registros existentes se conservaron.");
    return;
}

if (args.Contains("--crear-admin"))
{
    try { await AdministradorInicial.Ejecutar(app.Services); }
    catch (InvalidOperationException ex) { Console.Error.WriteLine(ex.Message); Environment.ExitCode = 1; }
    catch (MySqlConnector.MySqlException)
    {
        Console.Error.WriteLine("No se pudo preparar el administrador. Compruebe que MySQL esté iniciado, la base exista y la conexión tenga permisos para crear la tabla usuarios.");
        Environment.ExitCode = 1;
    }
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
