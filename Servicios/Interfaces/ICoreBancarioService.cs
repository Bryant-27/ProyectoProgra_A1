using DataAccess.Models;

namespace Servicios.Interfaces
{
    public interface ICoreBancarioService
    {
        // SRV19: Verificar si un cliente existe
        Task<bool> ClienteExisteAsync(string identificacion);

        // SRV15: Consultar saldo (por identificación + cuenta)
        Task<decimal?> ConsultarSaldoAsync(string identificacion, string numeroCuenta);

        // SRV16: Obtener últimos 5 movimientos
        Task<List<MovimientoCuenta>> ObtenerUltimosMovimientosAsync(string identificacion, string numeroCuenta);

        // SRV14: Aplicar transacción
        Task<(bool exito, string mensaje, decimal? nuevoSaldo)> AplicarTransaccionAsync(
            string identificacion,
            string tipoMovimiento,
            decimal monto,
            string? referenciaExterna = null,
            string? descripcion = null);

        // SRV13 - Consultar saldo por teléfono (usa afiliación + SRV15 internamente)
        Task<(bool exito, string mensaje, decimal? saldo)> ConsultarSaldoPorTelefonoAsync(
            string telefono,
            string identificacion);
    }
}