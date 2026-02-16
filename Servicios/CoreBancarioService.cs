using DataAccess.Models;
using Logica_Negocio.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Servicios.Interfaces;

namespace Servicios
{
    public class CoreBancarioService : ICoreBancarioService
    {
        private readonly CoreBancarioContext _context;
        private readonly IBitacoraService _bitacoraService;
        private readonly ITransaccionEnvioService _transaccionEnvioService;

        public CoreBancarioService(
            CoreBancarioContext context,
            IBitacoraService bitacoraService,
            ITransaccionEnvioService transaccionEnvioService)
        {
            _context = context;
            _bitacoraService = bitacoraService;
            _transaccionEnvioService = transaccionEnvioService;
        }

        // SRV19: Verificar si un cliente existe
        public async Task<bool> ClienteExisteAsync(string identificacion)
        {
            try
            {
                var existe = await _context.ClientesBanco
                    .AnyAsync(c => c.Identificacion == identificacion && c.IdEstado == 1);

                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: "Sistema",
                    accion: "CONSULTA_CLIENTE",
                    resultado: existe ? "EXITO" : "NO_ENCONTRADO",
                    descripcion: $"Consulta de existencia para identificación: {identificacion}",
                    servicio: "CoreBancarioService.ClienteExisteAsync"
                );

                return existe;
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: "Sistema",
                    accion: "CONSULTA_CLIENTE",
                    resultado: "ERROR",
                    descripcion: $"Error al consultar cliente {identificacion}: {ex.Message}",
                    servicio: "CoreBancarioService.ClienteExisteAsync"
                );
                throw;
            }
        }

        // SRV15: Consultar saldo
        public async Task<decimal?> ConsultarSaldoAsync(string identificacion, string numeroCuenta)
        {
            try
            {
                var cuenta = await _context.Cuentas
                    .Include(c => c.Cliente)
                    .FirstOrDefaultAsync(c => c.NumeroCuenta == numeroCuenta
                        && c.Cliente != null
                        && c.Cliente.Identificacion == identificacion
                        && c.IdEstado == 1);

                if (cuenta == null)
                {
                    await _bitacoraService.RegistrarAccionBitacora(
                        usuario: "Sistema",
                        accion: "CONSULTA_SALDO",
                        resultado: "NO_ENCONTRADO",
                        descripcion: $"Cuenta {numeroCuenta} no encontrada para identificación {identificacion}",
                        servicio: "CoreBancarioService.ConsultarSaldoAsync"
                    );
                    return null;
                }

                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: "Sistema",
                    accion: "CONSULTA_SALDO",
                    resultado: "EXITO",
                    descripcion: $"Saldo consultado para cuenta {numeroCuenta}: {cuenta.Saldo:C}",
                    servicio: "CoreBancarioService.ConsultarSaldoAsync"
                );

                return cuenta.Saldo;
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: "Sistema",
                    accion: "CONSULTA_SALDO",
                    resultado: "ERROR",
                    descripcion: $"Error al consultar saldo: {ex.Message}",
                    servicio: "CoreBancarioService.ConsultarSaldoAsync"
                );
                throw;
            }
        }

        // SRV16: Obtener últimos 5 movimientos
        public async Task<List<MovimientoCuenta>> ObtenerUltimosMovimientosAsync(string identificacion, string numeroCuenta)
        {
            try
            {
                var movimientos = await _context.MovimientosCuenta
                    .Include(m => m.Cuenta)
                        .ThenInclude(c => c!.Cliente)
                    .Where(m => m.NumeroCuenta == numeroCuenta
                        && m.Cuenta != null
                        && m.Cuenta.Cliente != null
                        && m.Cuenta.Cliente.Identificacion == identificacion)
                    .OrderByDescending(m => m.FechaMovimiento)
                    .Take(5)
                    .ToListAsync();

                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: "Sistema",
                    accion: "CONSULTA_MOVIMIENTOS",
                    resultado: "EXITO",
                    descripcion: $"Se consultaron {movimientos.Count} movimientos para cuenta {numeroCuenta}",
                    servicio: "CoreBancarioService.ObtenerUltimosMovimientosAsync"
                );

                return movimientos;
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: "Sistema",
                    accion: "CONSULTA_MOVIMIENTOS",
                    resultado: "ERROR",
                    descripcion: $"Error al consultar movimientos: {ex.Message}",
                    servicio: "CoreBancarioService.ObtenerUltimosMovimientosAsync"
                );
                throw;
            }
        }

        // SRV14: Aplicar transacción individual
        public async Task<(bool exito, string mensaje, decimal? nuevoSaldo)> AplicarTransaccionAsync(
            string identificacion,
            string tipoMovimiento,
            decimal monto,
            string? referenciaExterna = null,
            string? descripcion = null)
        {
            try
            {
                if (monto <= 0)
                {
                    return (false, "El monto debe ser mayor a cero", null);
                }

                var cliente = await _context.ClientesBanco
                    .Include(c => c.Cuentas.Where(cu => cu.IdEstado == 1))
                    .FirstOrDefaultAsync(c => c.Identificacion == identificacion && c.IdEstado == 1);

                if (cliente == null)
                {
                    return (false, "Cliente no existe o no está activo", null);
                }

                var cuenta = cliente.Cuentas.FirstOrDefault();
                if (cuenta == null)
                {
                    return (false, "Cliente no tiene cuentas activas", null);
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    decimal saldoAnterior = cuenta.Saldo;
                    decimal saldoNuevo;

                    if (tipoMovimiento.Equals("DEBITO", StringComparison.OrdinalIgnoreCase))
                    {
                        if (cuenta.Saldo < monto)
                        {
                            await transaction.RollbackAsync();
                            return (false, "Saldo insuficiente", cuenta.Saldo);
                        }
                        saldoNuevo = cuenta.Saldo - monto;
                    }
                    else if (tipoMovimiento.Equals("CREDITO", StringComparison.OrdinalIgnoreCase))
                    {
                        saldoNuevo = cuenta.Saldo + monto;
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return (false, "Tipo de movimiento inválido. Use CREDITO o DEBITO", null);
                    }

                    cuenta.Saldo = saldoNuevo;

                    var movimiento = new MovimientoCuenta
                    {
                        NumeroCuenta = cuenta.NumeroCuenta,
                        Monto = monto,
                        TipoMovimiento = tipoMovimiento.ToUpper(),
                        Descripcion = descripcion ?? $"Transacción {tipoMovimiento}",
                        SaldoAnterior = saldoAnterior,
                        SaldoNuevo = saldoNuevo,
                        ReferenciaExterna = referenciaExterna,
                        FechaMovimiento = DateTime.Now
                    };

                    _context.MovimientosCuenta.Add(movimiento);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    await _bitacoraService.RegistrarAccionBitacora(
                        usuario: "Sistema",
                        accion: "TRANSACCION",
                        resultado: "EXITO",
                        descripcion: $"{tipoMovimiento} por {monto:C} aplicado a cuenta {cuenta.NumeroCuenta}. Ref: {referenciaExterna}",
                        servicio: "CoreBancarioService.AplicarTransaccionAsync"
                    );

                    return (true, "Transacción aplicada exitosamente", saldoNuevo);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: "Sistema",
                    accion: "TRANSACCION",
                    resultado: "ERROR",
                    descripcion: $"Error al aplicar transacción: {ex.Message}",
                    servicio: "CoreBancarioService.AplicarTransaccionAsync"
                );
                throw;
            }
        }

        // Transferencia entre cuentas
        public async Task<(bool exito, string mensaje, decimal? saldoOrigenNuevo, decimal? saldoDestinoNuevo)> TransferirAsync(
        string identificacionOrigen,
        string cuentaOrigen,
        string identificacionDestino,
        string cuentaDestino,
        decimal monto,
        int entidadOrigenId,
        int entidadDestinoId,
        string telefonoOrigen,
        string nombreOrigen,
        string telefonoDestino,
        string? referenciaExterna = null,
        string? descripcion = null)
        {
            try
            {
                if (monto <= 0)
                {
                    return (false, "El monto debe ser mayor a cero", null, null);
                }

                var origen = await _context.Cuentas
                    .Include(c => c.Cliente)
                    .FirstOrDefaultAsync(c => c.NumeroCuenta == cuentaOrigen
                        && c.Cliente != null
                        && c.Cliente.Identificacion == identificacionOrigen
                        && c.IdEstado == 1);

                if (origen == null)
                {
                    return (false, "Cuenta origen no encontrada o no está activa", null, null);
                }

                var destino = await _context.Cuentas
                    .Include(c => c.Cliente)
                    .FirstOrDefaultAsync(c => c.NumeroCuenta == cuentaDestino
                        && c.Cliente != null
                        && c.Cliente.Identificacion == identificacionDestino
                        && c.IdEstado == 1);

                if (destino == null)
                {
                    return (false, "Cuenta destino no encontrada o no está activa", null, null);
                }

                if (origen.Saldo < monto)
                {
                    return (false, "Saldo insuficiente en cuenta origen", origen.Saldo, null);
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    decimal saldoOrigenAnterior = origen.Saldo;
                    decimal saldoDestinoAnterior = destino.Saldo;

                    origen.Saldo -= monto;
                    destino.Saldo += monto;

                    var movimientoOrigen = new MovimientoCuenta
                    {
                        NumeroCuenta = origen.NumeroCuenta,
                        Monto = monto,
                        TipoMovimiento = "DEBITO",
                        Descripcion = descripcion ?? $"Transferencia a cuenta {cuentaDestino}",
                        SaldoAnterior = saldoOrigenAnterior,
                        SaldoNuevo = origen.Saldo,
                        ReferenciaExterna = referenciaExterna,
                        FechaMovimiento = DateTime.Now
                    };

                    var movimientoDestino = new MovimientoCuenta
                    {
                        NumeroCuenta = destino.NumeroCuenta,
                        Monto = monto,
                        TipoMovimiento = "CREDITO",
                        Descripcion = descripcion ?? $"Transferencia desde cuenta {cuentaOrigen}",
                        SaldoAnterior = saldoDestinoAnterior,
                        SaldoNuevo = destino.Saldo,
                        ReferenciaExterna = referenciaExterna,
                        FechaMovimiento = DateTime.Now
                    };

                    _context.MovimientosCuenta.AddRange(movimientoOrigen, movimientoDestino);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Registrar en Transaccion_Envio con los datos correctos
                    try
                    {
                        await _transaccionEnvioService.RegistrarTransferenciaAsync(
                            entidadOrigenId: entidadOrigenId,
                            entidadDestinoId: entidadDestinoId,
                            telefonoOrigen: telefonoOrigen,
                            nombreOrigen: nombreOrigen,
                            telefonoDestino: telefonoDestino,
                            monto: monto,
                            descripcion: descripcion ?? "Transferencia",
                            codigoRespuesta: 200,  // Código HTTP de éxito
                            mensajeRespuesta: "Transferencia exitosa"
                        );
                    }
                    catch (Exception ex)
                    {
                        // Solo bitácora, no afectamos la respuesta al cliente
                        await _bitacoraService.RegistrarAccionBitacora(
                            usuario: "Sistema",
                            accion: "TRANSACCION_ENVIO",
                            resultado: "ERROR",
                            descripcion: $"Error registrando en Transaccion_Envio: {ex.Message}",
                            servicio: "CoreBancarioService.TransferirAsync"
                        );
                    }

                    await _bitacoraService.RegistrarAccionBitacora(
                        usuario: "Sistema",
                        accion: "TRANSFERENCIA",
                        resultado: "EXITO",
                        descripcion: $"Transferencia de {monto:C} desde {cuentaOrigen} hacia {cuentaDestino}. Ref: {referenciaExterna}",
                        servicio: "CoreBancarioService.TransferirAsync"
                    );

                    return (true, "Transferencia realizada exitosamente", origen.Saldo, destino.Saldo);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: "Sistema",
                    accion: "TRANSFERENCIA",
                    resultado: "ERROR",
                    descripcion: $"Error al realizar transferencia: {ex.Message}",
                    servicio: "CoreBancarioService.TransferirAsync"
                );
                throw;
            }
        }
    }
}