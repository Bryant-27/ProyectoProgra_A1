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
        private readonly IAfiliacionService _afiliacionService;

        public CoreBancarioService(
            CoreBancarioContext context,
            IBitacoraService bitacoraService,
            IAfiliacionService afiliacionService)
        {
            _context = context;
            _bitacoraService = bitacoraService;
            _afiliacionService = afiliacionService;
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

        // SRV14: Aplicar transacción
        public async Task<(bool exito, string mensaje, decimal? nuevoSaldo)> AplicarTransaccionAsync(
            string identificacion,
            string tipoMovimiento,
            decimal monto,
            string? referenciaExterna = null,
            string? descripcion = null)
        {
            try
            {
                // Validar monto
                if (monto <= 0)
                {
                    return (false, "El monto debe ser mayor a cero", null);
                }

                // Buscar cliente y su cuenta activa
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

                // Iniciar transacción
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    decimal saldoAnterior = cuenta.Saldo;
                    decimal saldoNuevo;

                    // Procesar según tipo de movimiento
                    if (tipoMovimiento.Equals("DEBITO", StringComparison.OrdinalIgnoreCase))
                    {
                        // Validar saldo suficiente para débito
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

                    // Actualizar saldo
                    cuenta.Saldo = saldoNuevo;

                    // Registrar movimiento
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

                    // Commit
                    await transaction.CommitAsync();

                    // Bitácora
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

        // SRV13: Consultar saldo por teléfono
        public async Task<(bool exito, string mensaje, decimal? saldo)> ConsultarSaldoPorTelefonoAsync(
    string telefono,
    string identificacion)
        {
            try
            {
                // Validar datos requeridos
                if (string.IsNullOrWhiteSpace(telefono))
                {
                    return (false, "Debe enviar los datos completos y válidos", null);
                }

                if (string.IsNullOrWhiteSpace(identificacion))
                {
                    return (false, "Debe enviar los datos completos y válidos", null);
                }

                // PASO 1: Validar que el teléfono esté asociado a pagos móviles
                var (existe, identificacionAfiliacion, nombre, cuenta) =
                    await _afiliacionService.ObtenerInfoPorTelefonoAsync(telefono);

                if (!existe)
                {
                    await _bitacoraService.RegistrarAccionBitacora(
                        usuario: "Sistema",
                        accion: "CONSULTA_SALDO_TELEFONO",
                        resultado: "NO_AFILIADO",  // Valor corto
                        descripcion: $"Teléfono {telefono} no está afiliado",
                        servicio: "CoreBancarioService.ConsultarSaldoPorTelefonoAsync"
                    );

                    return (false, "Cliente no asociado a pagos móviles", null);
                }

                // PASO 2: Validar que la identificación coincida
                if (identificacionAfiliacion != identificacion)
                {
                    await _bitacoraService.RegistrarAccionBitacora(
                        usuario: "Sistema",
                        accion: "CONSULTA_SALDO_TELEFONO",
                        resultado: "ID_NO_COINCIDE",  // Valor corto
                        descripcion: $"ID {identificacion} no coincide con telf {telefono}",
                        servicio: "CoreBancarioService.ConsultarSaldoPorTelefonoAsync"
                    );

                    return (false, "Identificación no coincide con el teléfono", null);
                }

                // PASO 3: Verificar que tenemos número de cuenta
                if (string.IsNullOrEmpty(cuenta))
                {
                    await _bitacoraService.RegistrarAccionBitacora(
                        usuario: "Sistema",
                        accion: "CONSULTA_SALDO_TELEFONO",
                        resultado: "ERROR_CUENTA",  // Valor corto
                        descripcion: $"Afiliación {telefono} sin cuenta",
                        servicio: "CoreBancarioService.ConsultarSaldoPorTelefonoAsync"
                    );

                    return (false, "Error en configuración de afiliación", null);
                }

                // PASO 4: Usar SRV15 para consultar el saldo en el core
                var saldo = await ConsultarSaldoAsync(identificacionAfiliacion, cuenta);

                // PASO 5: Verificar resultado
                if (saldo == null)
                {
                    await _bitacoraService.RegistrarAccionBitacora(
                        usuario: "Sistema",
                        accion: "CONSULTA_SALDO_TELEFONO",
                        resultado: "ERROR_CORE",  
                        descripcion: $"Error core para cuenta {cuenta}",
                        servicio: "CoreBancarioService.ConsultarSaldoPorTelefonoAsync"
                    );

                    return (false, "Error al consultar saldo en el core bancario", null);
                }

                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: "Sistema",
                    accion: "CONSULTA_SALDO_TELEFONO",
                    resultado: "EXITO",  
                    descripcion: $"Saldo telf {telefono}: {saldo:C}",
                    servicio: "CoreBancarioService.ConsultarSaldoPorTelefonoAsync"
                );

                return (true, "Consulta exitosa", saldo);
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: "Sistema",
                    accion: "CONSULTA_SALDO_TELEFONO",
                    resultado: "ERROR",  
                    descripcion: $"Error: {ex.Message}",
                    servicio: "CoreBancarioService.ConsultarSaldoPorTelefonoAsync"
                );

                return (false, $"Error interno: {ex.Message}", null);
            }
        }
    }
}