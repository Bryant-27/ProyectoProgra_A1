using DataAccess.Models;

namespace Logica_Negocio.Services
{
    public interface IAfiliacionService
    {
        Task<Afiliacion?> ObtenerPorTelefonoAsync(string telefono);
        Task<Entidades?> ObtenerEntidadPorIdAsync(int id);
    }
}
