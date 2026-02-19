using DataAccess.Models;
using DataAccess.Repositories;
//using Servicios.Interfaces;

namespace Logica_Negocio.Services
{
    public class AfiliacionService : IAfiliacionService
    {
        private readonly IAfiliacionRepository _afiliacionRepository;

        public AfiliacionService(IAfiliacionRepository afiliacionRepository)
        {
            _afiliacionRepository = afiliacionRepository;
        }

        public async Task<Afiliacion?> ObtenerPorTelefonoAsync(string telefono)
        {
            return await _afiliacionRepository.ObtenerPorTelefonoAsync(telefono);
        }

        public async Task<Entidades?> ObtenerEntidadPorIdAsync(int id)
        {
            return await _afiliacionRepository.ObtenerEntidadPorIdAsync(id);
        }
    }
}