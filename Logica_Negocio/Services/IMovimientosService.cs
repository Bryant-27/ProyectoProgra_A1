using Entities.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica_Negocio.Services
{
    public interface IMovimientosService
    {
        Task<MovimientosResponse> ObtenerUltimosMovimientosAsync(string telefono, string identificacion, string usuario);
    }
}
