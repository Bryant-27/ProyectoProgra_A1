using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica_Negocio.Services.Interfaces
{
    public interface IBitacoraService
    {
        Task RegistrarAccionBitacora(
            string usuario, 
            string accion, 
            string resultado, 
            string descripcion = "", 
            string servicio = "");
    }
}
