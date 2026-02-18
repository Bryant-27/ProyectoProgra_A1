using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica_Negocio.Services
{
    public interface IBitacoraService
    {
        Task RegistrarAsync(string usuario, string accion, string descripcion, string servicio, string resultado);
    }
}
