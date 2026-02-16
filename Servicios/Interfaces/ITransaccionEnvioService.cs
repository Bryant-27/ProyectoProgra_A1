using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servicios.Interfaces
{
    public interface ITransaccionEnvioService
    {
        Task RegistrarTransferenciaAsync(
            string telefonoOrigen,
            string nombreOrigen,
            string telefonoDestino,
            decimal monto,
            string descripcion,
            int codigoRespuesta,
            string mensajeRespuesta);
    }
}