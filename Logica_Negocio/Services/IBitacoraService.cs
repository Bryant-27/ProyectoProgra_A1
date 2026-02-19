using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica_Negocio.Services
{
    public interface IBitacoraService
    {
        Task RegistrarAsync(
            string usuario,
            string accion,
            string resultado,
            string descripcion = "",
            string servicio = "");
    }
}
