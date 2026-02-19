using Logica_Negocio.Models;

namespace Logica_Negocio.Interfaces
{
    public interface IAutenticacionService
    {
        Task<SesionUsuario?> ValidarYAutenticar(string idUsuario, string nombre, string password);
        Task<SesionUsuario?> RefrescarToken(string refreshToken);
        Task<bool> ValidarToken(string token);
        Task CerrarSesion(int idSession);
    }
}