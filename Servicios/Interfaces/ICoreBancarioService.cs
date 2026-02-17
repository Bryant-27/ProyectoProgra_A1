using DataAccess.Models;

namespace Servicios.Interfaces
{
    public interface ICoreBancarioService
    {
        // SRV19: Verificar si un cliente existe
        Task<bool> ClienteExisteAsync(string identificacion);

        // SRV15: Consultar saldo
        Task<decimal?> ConsultarSaldoAsync(string identificacion, string numeroCuenta);

        // SRV16: Obtener últimos 5 movimientos
        Task<List<MovimientoCuenta>> ObtenerUltimosMovimientosAsync(string identificacion, string numeroCuenta);

        // SRV14: Aplicar transacción individual (débito/crédito)
        Task<(bool exito, string mensaje, decimal? nuevoSaldo)> AplicarTransaccionAsync(
            string identificacion,
            string tipoMovimiento,
            decimal monto,
            string? referenciaExterna = null,
            string? descripcion = null);
    }
}