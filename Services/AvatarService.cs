namespace Inmobiliaria.Services;

public class AvatarService(IWebHostEnvironment entorno, ILogger<AvatarService> logger)
{
    private readonly string carpeta = Path.Combine(entorno.ContentRootPath, "App_Data", "avatares");
    public const long MaximoBytes = 2 * 1024 * 1024;

    public async Task<string> Guardar(IFormFile archivo)
    {
        if (archivo.Length == 0 || archivo.Length > MaximoBytes)
            throw new InvalidOperationException("El avatar debe pesar entre 1 byte y 2 MB.");
        await using var entrada = archivo.OpenReadStream();
        using var datos = new MemoryStream();
        var buffer = new byte[8192];
        int leidos;
        while ((leidos = await entrada.ReadAsync(buffer)) > 0)
        {
            if (datos.Length + leidos > MaximoBytes) throw new InvalidOperationException("El avatar supera los 2 MB.");
            await datos.WriteAsync(buffer.AsMemory(0, leidos));
        }
        var bytes = datos.ToArray();
        var extension = ExtensionImagen(bytes);
        if (extension is null) throw new InvalidOperationException("Seleccione una imagen PNG, JPEG o WebP válida. No se admite SVG.");
        Directory.CreateDirectory(carpeta);
        var nombre = Guid.NewGuid().ToString("N") + extension;
        await File.WriteAllBytesAsync(Path.Combine(carpeta, nombre), bytes);
        return nombre;
    }

    public static string? ExtensionImagen(byte[] bytes)
    {
        if (bytes.Length >= 24 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137, 80, 78, 71, 13, 10, 26, 10})
            && bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8)) return ".png";
        if (bytes.Length >= 4 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff) return ".jpg";
        if (bytes.Length >= 16 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return ".webp";
        return null;
    }

    public string? Ruta(string? nombre)
    {
        if (string.IsNullOrEmpty(nombre) || Path.GetFileName(nombre) != nombre ||
            !new[] { ".png", ".jpg", ".webp" }.Contains(Path.GetExtension(nombre))) return null;
        return Path.Combine(carpeta, nombre);
    }

    public void Eliminar(string? nombre)
    {
        var ruta = Ruta(nombre);
        try
        {
            if (ruta is not null && File.Exists(ruta)) File.Delete(ruta);
        }
        catch (IOException ex) { logger.LogWarning(ex, "No se pudo limpiar un avatar reemplazado."); }
        catch (UnauthorizedAccessException ex) { logger.LogWarning(ex, "No se pudo limpiar un avatar reemplazado."); }
    }
}
