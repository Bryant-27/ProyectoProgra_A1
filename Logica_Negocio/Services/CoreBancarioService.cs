using DataAccess.Models;
using Logica_Negocio.Interfaces;  // ← Cambiado
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logica_Negocio.Services
{
    public class CoreBancarioService : ICoreBancarioService
    {
        private readonly CoreBancarioContext _context;
        private readonly ILogger<CoreBancarioService> _logger;
        private readonly IAfiliacionService _afiliacionService;  // ← Agregar

        public CoreBancarioService(
            CoreBancarioContext context,
            ILogger<CoreBancarioService> logger,
            IAfiliacionService afiliacionService)  // ← Agregar
        {
            _context = context;
            _logger = logger;
            _afiliacionService = afiliacionService;
        }

        // SRV19: Verificar si un cliente existe
        public async Task<bool> ClienteExisteAsync(string identificacion)
        {
            try
            {
                _logger.LogInformation("Verificando cliente con ID: {Identificacion}", identificacion);
                return await _context.ClientesBanco
                    .AnyAsync(c => c.Identificacion == identificacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando cliente {Identificacion}", identificacion);
                throw;
            }
        }

        // SRV15: Consultar saldo
        public async Task<decimal?> ConsultarSaldoAsync(string identificacion, string numeroCuenta)
        {
            try
            {
                _logger.LogInformation("Consultando saldo cuenta {NumeroCuenta}", numeroCuenta);

                var cliente = await _context.ClientesBanco
                    .Include(c => c.Cuentas)
                    .FirstOrDefaultAsync(c => c.Identificacion == identificacion);

                if (cliente == null)
                {
                    _logger.LogWarning("Cliente no encontrado: {Identificacion}", identificacion);
                    return null;
                }

                var cuenta = cliente.Cuentas?.FirstOrDefault(c => c.NumeroCuenta == numeroCuenta);

                if (cuenta == null)
                {
                    _logger.LogWarning("Cuenta {NumeroCuenta} no encontrada", numeroCuenta);
                    return null;
                }

                return cuenta.Saldo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando saldo cuenta {NumeroCuenta}", numeroCuenta);
                throw;
            }
        }

        // SRV16: Obtener últimos 5 movimientos
        public async Task<List<MovimientoCuenta>> ObtenerUltimosMovimientosAsync(string identificacion, string numeroCuenta)
        {
            try
            {
                _logger.LogInformation("Obteniendo movimientos para cuenta {NumeroCuenta}", numeroCuenta);

                return await _context.MovimientosCuenta
                    .Where(m => m.NumeroCuenta == numeroCuenta)
                    .OrderByDescending(m => m.FechaMovimiento)
                    .Take(5)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo movimientos cuenta {NumeroCuenta}", numeroCuenta);
                throw;
            }
        }

        // SRV14: Aplicar transacción
        public async Task<TransaccionResultado> AplicarTransaccionAsync(
            string identificacion,
            string tipoMovimiento,
            decimal monto,
            string? referenciaExterna = null,
            string? descripcion = null)
        {
            try
            {
                _logger.LogInformation("Aplicando {TipoMovimiento} por {Monto:C}", tipoMovimiento, monto);

                var cliente = await _context.ClientesBanco
                    .Include(c => c.Cuentas)
                    .FirstOrDefaultAsync(c => c.Identificacion == identificacion);

                if (cliente == null)
                {
                    return new TransaccionResultado
                    {
                        Exito = false,
                        Mensaje = "Cliente no encontrado",
                        NuevoSaldo = null
                    };
                }

                var cuenta = cliente.Cuentas?.FirstOrDefault();
                if (cuenta == null)
                {
                    return new TransaccionResultado
                    {
                        Exito = false,
                        Mensaje = "Cliente no tiene cuentas",
                        NuevoSaldo = null
                    };
                }

                if (monto <= 0)
                {
                    return new TransaccionResultado
                    {
                        Exito = false,
                        Mensaje = "Monto debe ser mayor a cero",
                        NuevoSaldo = cuenta.Saldo
                    };
                }

                decimal nuevoSaldo;
                if (tipoMovimiento.ToUpper() == "CREDITO")
                {
                    nuevoSaldo = cuenta.Saldo + monto;
                }
                else if (tipoMovimiento.ToUpper() == "DEBITO")
                {
                    if (cuenta.Saldo < monto)
                    {
                        return new TransaccionResultado
                        {
                            Exito = false,
                            Mensaje = "Saldo insuficiente",
                            NuevoSaldo = cuenta.Saldo
                        };
                    }
                    nuevoSaldo = cuenta.Saldo - monto;
                }
                else
                {
                    return new TransaccionResultado
                    {
                        Exito = false,
                        Mensaje = "Use CREDITO o DEBITO",
                        NuevoSaldo = cuenta.Saldo
                    };
                }

                var movimiento = new MovimientoCuenta
                {
                    NumeroCuenta = cuenta.NumeroCuenta,
                    FechaMovimiento = DateTime.Now,
                    Monto = monto,
                    TipoMovimiento = tipoMovimiento.ToUpper(),
                    SaldoAnterior = cuenta.Saldo,
                    SaldoNuevo = nuevoSaldo,
                    ReferenciaExterna = referenciaExterna,
                    Descripcion = descripcion ?? $"Transacción {tipoMovimiento}"
                };

                cuenta.Saldo = nuevoSaldo;
                _context.MovimientosCuenta.Add(movimiento);
                await _context.SaveChangesAsync();

                return new TransaccionResultado
                {
                    Exito = true,
                    Mensaje = "Transacción exitosa",
                    NuevoSaldo = nuevoSaldo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aplicando transacción");
                return new TransaccionResultado
                {
                    Exito = false,
                    Mensaje = $"Error: {ex.Message}",
                    NuevoSaldo = null
                };
            }
        }

        // SRV13: Consultar saldo por teléfono (VERSIÓN CORREGIDA)
        public async Task<ConsultaSaldoResultado> ConsultarSaldoPorTelefonoAsync(string telefono, string identificacion)
        {
            try
            {
                _logger.LogInformation("Consultando saldo por teléfono {Telefono}", telefono);

                // Buscar afiliación por teléfono
                var afiliacion = await _afiliacionService.ObtenerPorTelefonoAsync(telefono);

                if (afiliacion == null)
                {
                    return new ConsultaSaldoResultado
                    {
                        Exito = false,
                        Mensaje = "Teléfono no afiliado",
                        Saldo = null
                    };
                }

                // Validar que la identificación coincida
                // NOTA: Ajusta estos nombres según tu modelo real
                string identificacionAfiliacion = "";
                string numeroCuenta = "";

                // Intenta con diferentes nombres de propiedades
                try
                {
                    // Intenta con propiedades comunes
                    var propId = afiliacion.GetType().GetProperty("IdentificacionUsuario") ??
                                afiliacion.GetType().GetProperty("Identificacion_Usuario") ??
                                afiliacion.GetType().GetProperty("Identificacion");

                    var propCuenta = afiliacion.GetType().GetProperty("NumeroCuenta") ??
                                    afiliacion.GetType().GetProperty("Numero_Cuenta") ??
                                    afiliacion.GetType().GetProperty("Cuenta");

                    if (propId != null)
                        identificacionAfiliacion = propId.GetValue(afiliacion)?.ToString() ?? "";

                    if (propCuenta != null)
                        numeroCuenta = propCuenta.GetValue(afiliacion)?.ToString() ?? "";
                }
                catch
                {
                    // Si falla la reflexión, asumimos nombres específicos
                    // CAMBIA ESTOS NOMBRES SEGÚN TU MODELO REAL
                    identificacionAfiliacion = afiliacion.IdentificacionUsuario; // Prueba con esto
                    numeroCuenta = afiliacion.NumeroCuenta; // Prueba con esto
                }

                if (identificacionAfiliacion != identificacion)
                {
                    return new ConsultaSaldoResultado
                    {
                        Exito = false,
                        Mensaje = "Identificación no coincide con el teléfono",
                        Saldo = null
                    };
                }

                if (string.IsNullOrEmpty(numeroCuenta))
                {
                    return new ConsultaSaldoResultado
                    {
                        Exito = false,
                        Mensaje = "La afiliación no tiene número de cuenta",
                        Saldo = null
                    };
                }

                // Consultar saldo usando SRV15
                var saldo = await ConsultarSaldoAsync(identificacion, numeroCuenta);

                if (saldo == null)
                {
                    return new ConsultaSaldoResultado
                    {
                        Exito = false,
                        Mensaje = "Error al consultar saldo en el core bancario",
                        Saldo = null
                    };
                }

                return new ConsultaSaldoResultado
                {
                    Exito = true,
                    Mensaje = "Consulta exitosa",
                    Saldo = saldo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando saldo por teléfono");
                return new ConsultaSaldoResultado
                {
                    Exito = false,
                    Mensaje = $"Error interno: {ex.Message}",
                    Saldo = null
                };
            }
        }

    }
}