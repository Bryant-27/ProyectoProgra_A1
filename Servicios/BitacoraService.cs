using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Servicios.Interfaces;
using DataAccess.Models;
//using Logica_Negocio.Services.Interfaces;


namespace Logica_Negocio.Services.Interfaces
{
    public class BitacoraService : IBitacoraService
    {
        private readonly DBContext_Bitacora _bitacoraContext;

        public BitacoraService(DBContext_Bitacora bitacoraContext)
        {
            _bitacoraContext = bitacoraContext;
        }

        public async Task RegistrarAccionBitacora(
        string usuario,
        string accion,
        string resultado,
        string descripcion,
        string servicio)
        {
            var bitacora = new BitacoraMovimiento
            {
                Usuario = usuario,
                Accion = accion,
                Resultado = resultado,
                Descripcion = descripcion,
                Servicio = servicio,
                FechaRegistro = DateTime.UtcNow
            };

            _bitacoraContext.Bitacora.Add(bitacora);
            await _bitacoraContext.SaveChangesAsync();
        }

        //public Task RegistrarAccionBitacora(string usuario, string accion, string resultado, string descripcion = "", string servicio = "")
        //{
        //    throw new NotImplementedException();
        //}
    }
}
