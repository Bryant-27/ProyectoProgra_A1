using DataAccess.Models;

namespace Logica_Negocio.Interfaces
{
    public interface ICoreBancarioService
    {
        Task<bool> ClienteExisteAsync(string identificacion);
        Task<decimal?> ConsultarSaldoAsync(string identificacion, string numeroCuenta);
        Task<List<MovimientoCuenta>> ObtenerUltimosMovimientosAsync(string identificacion, string numeroCuenta);
        Task<TransaccionResultado> AplicarTransaccionAsync(
            string identificacion,
            string tipoMovimiento,
            decimal monto,
            string? referenciaExterna = null,
            string? descripcion = null);
        Task<ConsultaSaldoResultado> ConsultarSaldoPorTelefonoAsync(string telefono, string identificacion);
    }

    public class TransaccionResultado
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = null!;
        public decimal? NuevoSaldo { get; set; }
    }

    public class ConsultaSaldoResultado
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = null!;
        public decimal? Saldo { get; set; }
    }
}