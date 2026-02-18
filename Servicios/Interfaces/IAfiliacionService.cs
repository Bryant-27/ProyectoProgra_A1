using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servicios.Interfaces
{
    public interface IAfiliacionService
    {
        Task<(bool existe, string? identificacion, string? nombre, string? numeroCuenta)> ObtenerInfoPorTelefonoAsync(string telefono);
    }
}