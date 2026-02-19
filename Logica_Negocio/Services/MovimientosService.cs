using DataAccess.Repositories;
using DataAccess.Models;         
using Entities.DTOs;
using Microsoft.EntityFrameworkCore; 

namespace Logica_Negocio.Services
{
    public class MovimientosService : IMovimientosService
    {
        private readonly IAfiliacionRepository _afiliacionRepository;
        private readonly IBitacoraService _bitacoraService;
        private readonly CoreBancarioContext _coreContext; //  Inyectar contexto directo

        public MovimientosService(
            IAfiliacionRepository afiliacionRepository,
            IBitacoraService bitacoraService,
            CoreBancarioContext coreContext) 
        {
            _afiliacionRepository = afiliacionRepository;
            _bitacoraService = bitacoraService;
            _coreContext = coreContext; 
        }

        public async Task<MovimientosResponse> ObtenerUltimosMovimientosAsync(
            string telefono, string identificacion, string usuario)
        {
            // ... validaciones igual que tienes ...

            try
            {
                var afiliacion = await _afiliacionRepository
                    .GetByTelefonoAndIdentificacionAsync(telefono, identificacion);

                if (afiliacion == null)
                {
                    // ... igual que tienes ...
                    return new MovimientosResponse
                    {
                        Codigo = -1,
                        Descripcion = "Cliente no asociado a pagos móviles",
                        Movimientos = new List<MovimientoDto>()
                    };
                }

                // ← CAMBIO AQUÍ: Consulta directa a la base de datos Core
                var movimientos = await _coreContext.MovimientosCuenta
                    .AsNoTracking()
                    .Include(m => m.Cuenta)
                        .ThenInclude(c => c.Cliente)
                    .Where(m => m.NumeroCuenta == afiliacion.NumeroCuenta &&
                                m.Cuenta.Cliente.Identificacion == afiliacion.IdentificacionUsuario)
                    .OrderByDescending(m => m.FechaMovimiento)
                    .Take(5)
                    .Select(m => new MovimientoDto
                    {
                        MovimientoId = m.MovimientoId,
                        FechaMovimiento = m.FechaMovimiento,
                        Monto = m.Monto,
                        TipoMovimiento = m.TipoMovimiento,
                        Descripcion = m.Descripcion ?? string.Empty,
                        SaldoAnterior = m.SaldoAnterior ?? 0,
                        SaldoNuevo = m.SaldoNuevo ?? 0
                    })
                    .ToListAsync();

                if (!movimientos.Any())
                {
                    return new MovimientosResponse
                    {
                        Codigo = 0,
                        Descripcion = "No se encontraron movimientos",
                        Movimientos = new List<MovimientoDto>()
                    };
                }

                await _bitacoraService.RegistrarAsync(
                    usuario, "CONSULTA_MOVIMIENTOS", "EXITO",
                    $"Cuenta: {afiliacion.NumeroCuenta}, Movs: {movimientos.Count}", "SRV11");

                return new MovimientosResponse
                {
                    Codigo = 0,
                    Descripcion = "Consulta exitosa",
                    Movimientos = movimientos
                };
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAsync(
                    usuario: usuario,
                    accion: "CONSULTA_MOVIMIENTOS",
                    resultado: "ERROR",        
                    descripcion: ex.Message,     
                    servicio: "SRV11");
                        return new MovimientosResponse
                {
                    Codigo = -1,
                    Descripcion = "Error al consultar movimientos",
                    Movimientos = new List<MovimientoDto>()
                };
            }
        }
    }
}