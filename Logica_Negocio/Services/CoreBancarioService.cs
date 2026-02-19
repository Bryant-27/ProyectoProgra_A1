using Abstract.Interfaces;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Logica_Negocio.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logica_Negocio.Services
{
    public class CoreBancarioService : ICoreBancarioService
    {
        private readonly CoreBancarioContext _context;
        private readonly ILogger<CoreBancarioService> _logger;

        public CoreBancarioService(
            CoreBancarioContext context,
            ILogger<CoreBancarioService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Verifica si un cliente existe por su identificación
        /// </summary>
        public async Task<bool> ClienteExisteAsync(string identificacion)
        {
            try
            {
                _logger.LogInformation("Verificando si existe cliente con identificación: {Identificacion}", identificacion);

                // Verificar qué nombre de tabla tienes en tu DbContext
                // Opciones posibles:
                return await _context.Set<ClienteBanco>().AnyAsync(c => c.Identificacion == identificacion);
                // O si tienes un DbSet específico:
                // return await _context.Clientes.AnyAsync(c => c.Identificacion == identificacion);
                // return await _context.Usuarios.AnyAsync(c => c.Identificacion == identificacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando cliente con identificación {Identificacion}", identificacion);
                throw;
            }
        }

        /// <summary>
        /// Consulta el saldo de una cuenta específica
        /// </summary>
        public async Task<decimal?> ConsultarSaldoAsync(string identificacion, string numeroCuenta)
        {
            try
            {
                _logger.LogInformation("Consultando saldo para cuenta {NumeroCuenta} del cliente {Identificacion}",
                    numeroCuenta, identificacion);

                // Buscar cliente con sus cuentas
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
                    _logger.LogWarning("Cuenta {NumeroCuenta} no encontrada para el cliente", numeroCuenta);
                    return null;
                }

                return cuenta.Saldo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando saldo para cuenta {NumeroCuenta}", numeroCuenta);
                throw;
            }
        }

        /// Obtiene los últimos 10 movimientos de una cuenta
       
        public async Task<List<MovimientoCuenta>> ObtenerUltimosMovimientosAsync(string identificacion, string numeroCuenta)
        {
            try
            {
                _logger.LogInformation("Obteniendo últimos movimientos para cuenta {NumeroCuenta}", numeroCuenta);

                return await _context.MovimientosCuenta
                    .Where(m => m.Identificacion == identificacion && m.NumeroCuenta == numeroCuenta)
                    .OrderByDescending(m => m.FechaMovimiento)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo movimientos para cuenta {NumeroCuenta}", numeroCuenta);
                throw;
            }
        }

        /// <summary>
        /// Aplica una transacción (crédito/débito) a una cuenta
  
        public async Task<TransaccionResultado> AplicarTransaccionAsync(
            string identificacion,
            string tipoMovimiento,
            decimal monto,
            string? referenciaExterna = null,
            string? descripcion = null)
        {
            try
            {
                _logger.LogInformation("Aplicando transacción {TipoMovimiento} por ₡{Monto} al cliente {Identificacion}",
                    tipoMovimiento, monto, identificacion);

                // Buscar cliente
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

                // Obtener primera cuenta del cliente (o puedes hacer que reciba número de cuenta)
                var cuenta = cliente.Cuentas?.FirstOrDefault();
                if (cuenta == null)
                {
                    return new TransaccionResultado
                    {
                        Exito = false,
                        Mensaje = "Cliente no tiene cuentas asociadas",
                        NuevoSaldo = null
                    };
                }

                // Validar monto
                if (monto <= 0)
                {
                    return new TransaccionResultado
                    {
                        Exito = false,
                        Mensaje = "El monto debe ser mayor a cero",
                        NuevoSaldo = cuenta.Saldo
                    };
                }

                // Aplicar según tipo de movimiento
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
                        Mensaje = "Tipo de movimiento inválido. Use CREDITO o DEBITO",
                        NuevoSaldo = cuenta.Saldo
                    };
                }

                // Crear movimiento
                var movimiento = new MovimientoCuenta
                {
                    NumeroCuenta = cuenta.NumeroCuenta,
                    Identificacion = identificacion,
                    FechaMovimiento = DateTime.Now,
                    Monto = monto,
                    TipoMovimiento = tipoMovimiento.ToUpper(),
                    SaldoAnterior = cuenta.Saldo,
                    SaldoNuevo = nuevoSaldo,
                    ReferenciaExterna = referenciaExterna,
                    Descripcion = descripcion ?? $"Transacción {tipoMovimiento}"
                };

                // Actualizar saldo de la cuenta
                cuenta.Saldo = nuevoSaldo;

                // Guardar cambios
                _context.MovimientosCuenta.Add(movimiento);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Transacción aplicada exitosamente. Nuevo saldo: ₡{NuevoSaldo}", nuevoSaldo);

                return new TransaccionResultado
                {
                    Exito = true,
                    Mensaje = "Transacción aplicada exitosamente",
                    NuevoSaldo = nuevoSaldo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aplicando transacción para cliente {Identificacion}", identificacion);

                return new TransaccionResultado
                {
                    Exito = false,
                    Mensaje = $"Error interno: {ex.Message}",
                    NuevoSaldo = null
                };
            }
        }

        /// <summary>
        /// Consulta saldo por número de teléfono
        /// </summary>
        public async Task<ConsultaSaldoResultado> ConsultarSaldoPorTelefonoAsync(string telefono, string identificacion)
        {
            try
            {
                _logger.LogInformation("Consultando saldo por teléfono {Telefono} para cliente {Identificacion}",
                    telefono, identificacion);

                // Buscar cliente
                var cliente = await _context.ClientesBanco
                    .Include(c => c.Cuentas)
                    .FirstOrDefaultAsync(c => c.Identificacion == identificacion);

                if (cliente == null)
                {
                    return new ConsultaSaldoResultado
                    {
                        Exito = false,
                        Mensaje = "Cliente no encontrado",
                        Saldo = null
                    };
                }

                // Aquí podrías buscar por teléfono en alguna tabla de afiliaciones
                // Por ahora, retornamos el saldo de la primera cuenta
                var cuenta = cliente.Cuentas?.FirstOrDefault();

                return new ConsultaSaldoResultado
                {
                    Exito = true,
                    Mensaje = "Consulta exitosa",
                    Saldo = cuenta?.Saldo ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando saldo por teléfono {Telefono}", telefono);

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