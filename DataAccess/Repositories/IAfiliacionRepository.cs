using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public interface IAfiliacionRepository
    {
        Task<Afiliacion> GetByTelefonoAndIdentificacionAsync(string telefono, string identificacion);
        Task<Entidades?> ObtenerEntidadPorIdAsync(int id);
        Task<Afiliacion?> ObtenerPorTelefonoAsync(string telefono);
    }
}
