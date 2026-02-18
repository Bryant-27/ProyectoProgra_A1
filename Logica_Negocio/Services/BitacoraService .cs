using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica_Negocio.Services
{
    public class BitacoraService : IBitacoraService
    {
        private readonly DBContext_Bitacora _context;

        public BitacoraService(DBContext_Bitacora context)
        {
            _context = context;
        }

        public async Task RegistrarAsync(string usuario, string accion, string descripcion, string servicio, string resultado)
        {
            var bitacora = new BitacoraMovimiento
            {
                Usuario = usuario,
                Accion = accion,
                Descripcion = descripcion,
                FechaRegistro = DateTime.UtcNow,
                Servicio = servicio,
                Resultado = resultado
            };

            _context.Bitacora.Add(bitacora);
            await _context.SaveChangesAsync();
        }
    }
}
