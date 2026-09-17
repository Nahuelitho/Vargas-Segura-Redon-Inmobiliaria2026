using Inmobiliaria.Models;

namespace Inmobiliaria.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorId(int id);
    Task<Usuario?> ObtenerPorEmail(string email);
    Task<UsuariosListado> Listar(int pagina, string? buscar);
    Task<int> Crear(Usuario usuario, bool primerAdministrador = false);
    Task Actualizar(Usuario usuario);
    Task ActualizarPerfil(int id, PerfilFormulario perfil, string? avatar);
    Task CambiarPassword(int id, string hash, string selloAnterior);
    Task Eliminar(int id);
}
