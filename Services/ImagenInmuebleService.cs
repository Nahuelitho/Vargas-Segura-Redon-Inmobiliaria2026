namespace Inmobiliaria.Services;

public class ImagenInmuebleService(IWebHostEnvironment entorno, ILogger<ImagenInmuebleService> logger)
{
    private readonly string _carpeta = Path.Combine(entorno.WebRootPath, "images", "inmuebles");
    public const long MaximoBytes = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Guarda el archivo en wwwroot/images/inmuebles/ y devuelve el nombre asignado.
    /// Lanza InvalidOperationException si el archivo es invalido.
    /// </summary>
    public async Task<string> Guardar(IFormFile archivo)
    {
        if (archivo.Length == 0 || archivo.Length > MaximoBytes)
            throw new InvalidOperationException("La imagen debe pesar entre 1 byte y 5 MB.");

        await using var entrada = archivo.OpenReadStream();
        using var datos = new MemoryStream();
        var buffer = new byte[8192];
        int leidos;
        while ((leidos = await entrada.ReadAsync(buffer)) > 0)
        {
            if (datos.Length + leidos > MaximoBytes)
                throw new InvalidOperationException("La imagen supera los 5 MB.");
            await datos.WriteAsync(buffer.AsMemory(0, leidos));
        }

        var bytes = datos.ToArray();
        var extension = ExtensionImagen(bytes);
        if (extension is null)
            throw new InvalidOperationException("Seleccione una imagen PNG, JPEG o WebP valida.");

        Directory.CreateDirectory(_carpeta);
        var nombre = Guid.NewGuid().ToString("N") + extension;
        await File.WriteAllBytesAsync(Path.Combine(_carpeta, nombre), bytes);
        return nombre;
    }

    /// <summary>
    /// Elimina el archivo fisico. Silencia errores de IO.
    /// </summary>
    public void Eliminar(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return;
        if (nombre.Contains('/') || nombre.Contains('\\') || nombre.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return;
        var ruta = Path.Combine(_carpeta, nombre);
        try { if (File.Exists(ruta)) File.Delete(ruta); }
        catch (IOException ex) { logger.LogWarning(ex, "No se pudo eliminar la imagen {Nombre}.", nombre); }
        catch (UnauthorizedAccessException ex) { logger.LogWarning(ex, "Sin permisos para eliminar la imagen {Nombre}.", nombre); }
    }

    /// <summary>
    /// Detecta el tipo de imagen por magic bytes.
    /// Devuelve la extension con punto, o null si no es un formato aceptado.
    /// </summary>
    public static string? ExtensionImagen(byte[] bytes)
    {
        if (bytes.Length >= 24
            && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            && bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8))
            return ".png";
        if (bytes.Length >= 4 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff)
            return ".jpg";
        if (bytes.Length >= 16
            && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8)
            && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return ".webp";
        return null;
    }

    /// <summary>
    /// Devuelve la ruta web de la imagen (/images/inmuebles/{nombre}),
    /// o la URL original si ya era una URL externa (compatibilidad legada).
    /// </summary>
    public static string? RutaWeb(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return null;
        if (nombre.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return nombre;
        return "/images/inmuebles/" + nombre;
    }
}
