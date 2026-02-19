using DataAccess.Models;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public interface IAfiliacionRepository
    {
        Task<Afiliacion?> GetByTelefonoAndIdentificacionAsync(string telefono, string identificacion);
        Task<Entidades?> ObtenerEntidadPorIdAsync(int id);  // ← Cambiado a int
        Task<Afiliacion?> ObtenerPorTelefonoAsync(string telefono);
    }
}