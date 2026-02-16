using Logica_Negocio.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servicios;
using Servicios.DTOs;
using Servicios.Interfaces;
using System.Security.Claims;

namespace Proyecto_A1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CoreBancarioController : ControllerBase
    {
        private readonly ICoreBancarioService _coreService;
        private readonly IBitacoraService _bitacoraService;

        public CoreBancarioController(
            ICoreBancarioService coreService,
            IBitacoraService bitacoraService)
        {
            _coreService = coreService;
            _bitacoraService = bitacoraService;
        }

        private string ObtenerUsuarioActual()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? "Sistema";
        }

        // SRV19: Verificar si un cliente existe
        [HttpGet("cliente-existe")]
        public async Task<IActionResult> ClienteExiste([FromQuery] string identificacion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identificacion))
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "La identificación es requerida"
                    });
                }

                var existe = await _coreService.ClienteExisteAsync(identificacion);

                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_CLIENTE",
                    resultado: "EXITO",
                    descripcion: $"Consulta cliente existe: {identificacion} = {existe}",
                    servicio: "CoreBancarioController.ClienteExiste"
                );

                return Ok(new
                {
                    codigo = 0,
                    descripcion = "Consulta exitosa",
                    existe = existe
                });
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_CLIENTE",
                    resultado: "ERROR",
                    descripcion: $"Error: {ex.Message}",
                    servicio: "CoreBancarioController.ClienteExiste"
                );

                return StatusCode(500, new CoreResponseDto
                {
                    Codigo = -1,
                    Descripcion = "Error interno del servidor"
                });
            }
        }

        // SRV15: Consultar saldo
        [HttpGet("saldo")]
        public async Task<IActionResult> ConsultarSaldo(
            [FromQuery] string identificacion,
            [FromQuery] string cuenta)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identificacion) || string.IsNullOrWhiteSpace(cuenta))
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "Identificación y cuenta son requeridas"
                    });
                }

                var saldo = await _coreService.ConsultarSaldoAsync(identificacion, cuenta);

                if (saldo == null)
                {
                    return NotFound(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "Cliente o cuenta no encontrados"
                    });
                }

                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_SALDO",
                    resultado: "EXITO",
                    descripcion: $"Saldo consultado para cuenta {cuenta}: {saldo:C}",
                    servicio: "CoreBancarioController.ConsultarSaldo"
                );

                return Ok(new SaldoResponseDto
                {
                    Codigo = 0,
                    Descripcion = "Consulta exitosa",
                    Saldo = saldo
                });
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_SALDO",
                    resultado: "ERROR",
                    descripcion: $"Error: {ex.Message}",
                    servicio: "CoreBancarioController.ConsultarSaldo"
                );

                return StatusCode(500, new CoreResponseDto
                {
                    Codigo = -1,
                    Descripcion = "Error interno del servidor"
                });
            }
        }

        // SRV16: Obtener últimos movimientos
        [HttpGet("movimientos")]
        public async Task<IActionResult> ObtenerMovimientos(
            [FromQuery] string identificacion,
            [FromQuery] string cuenta)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identificacion) || string.IsNullOrWhiteSpace(cuenta))
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "Identificación y cuenta son requeridas"
                    });
                }

                var movimientos = await _coreService.ObtenerUltimosMovimientosAsync(identificacion, cuenta);

                var movimientosDto = movimientos.Select(m => new MovimientoDto
                {
                    Fecha = m.FechaMovimiento,
                    Tipo = m.TipoMovimiento,
                    Monto = m.Monto,
                    Descripcion = m.Descripcion,
                    SaldoAnterior = m.SaldoAnterior,
                    SaldoNuevo = m.SaldoNuevo
                }).ToList();

                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_MOVIMIENTOS",
                    resultado: "EXITO",
                    descripcion: $"Se consultaron {movimientos.Count} movimientos para cuenta {cuenta}",
                    servicio: "CoreBancarioController.ObtenerMovimientos"
                );

                return Ok(new MovimientosResponseDto
                {
                    Codigo = 0,
                    Descripcion = "Consulta exitosa",
                    Movimientos = movimientosDto
                });
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_MOVIMIENTOS",
                    resultado: "ERROR",
                    descripcion: $"Error: {ex.Message}",
                    servicio: "CoreBancarioController.ObtenerMovimientos"
                );

                return StatusCode(500, new CoreResponseDto
                {
                    Codigo = -1,
                    Descripcion = "Error interno del servidor"
                });
            }
        }

        // SRV14: Aplicar transacción individual
        [HttpPost("transaccion")]
        public async Task<IActionResult> AplicarTransaccion([FromBody] TransaccionRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "Datos de transacción requeridos"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Identificacion))
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "Identificación requerida"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.TipoMovimiento))
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "Tipo de movimiento requerido (CREDITO/DEBITO)"
                    });
                }

                if (request.Monto <= 0)
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "Monto debe ser mayor a cero"
                    });
                }

                var (exito, mensaje, nuevoSaldo) = await _coreService.AplicarTransaccionAsync(
                    request.Identificacion,
                    request.TipoMovimiento,
                    request.Monto,
                    request.ReferenciaExterna,
                    request.Descripcion
                );

                if (!exito)
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = mensaje
                    });
                }

                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: ObtenerUsuarioActual(),
                    accion: "TRANSACCION",
                    resultado: "EXITO",
                    descripcion: $"{request.TipoMovimiento} por {request.Monto:C} aplicado. Ref: {request.ReferenciaExterna}",
                    servicio: "CoreBancarioController.AplicarTransaccion"
                );

                return Ok(new
                {
                    codigo = 0,
                    descripcion = mensaje,
                    nuevoSaldo = nuevoSaldo
                });
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: ObtenerUsuarioActual(),
                    accion: "TRANSACCION",
                    resultado: "ERROR",
                    descripcion: $"Error: {ex.Message}",
                    servicio: "CoreBancarioController.AplicarTransaccion"
                );

                return StatusCode(500, new CoreResponseDto
                {
                    Codigo = -1,
                    Descripcion = "Error interno del servidor"
                });
            }
        }

        //Transferencia entre cuentas (con registro en Transaccion_Envio)
        [HttpPost("transferir")]
        public async Task<IActionResult> Transferir([FromBody] TransferenciaRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "Datos de transferencia requeridos"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.IdentificacionOrigen))
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "Identificación de origen requerida"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.CuentaOrigen))
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "Cuenta de origen requerida"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.IdentificacionDestino))
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "Identificación de destino requerida"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.CuentaDestino))
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "Cuenta de destino requerida"
                    });
                }

                if (request.Monto <= 0)
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "Monto debe ser mayor a cero"
                    });
                }

                if (request.CuentaOrigen == request.CuentaDestino &&
                    request.IdentificacionOrigen == request.IdentificacionDestino)
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "No se puede transferir a la misma cuenta"
                    });
                }

                // Procesar transferencia
                var (exito, mensaje, saldoOrigenNuevo, saldoDestinoNuevo) = await _coreService.TransferirAsync(
                    request.IdentificacionOrigen,
                    request.CuentaOrigen,
                    request.IdentificacionDestino,
                    request.CuentaDestino,
                    request.Monto,
                    request.ReferenciaExterna,
                    request.Descripcion
                );

                if (!exito)
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = mensaje
                    });
                }

                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: ObtenerUsuarioActual(),
                    accion: "TRANSFERENCIA",
                    resultado: "EXITO",
                    descripcion: $"Transferencia de {request.Monto:C} de {request.CuentaOrigen} a {request.CuentaDestino}",
                    servicio: "CoreBancarioController.Transferir"
                );

                return Ok(new TransferenciaResponseDto
                {
                    Codigo = 0,
                    Descripcion = mensaje,
                    SaldoOrigenNuevo = saldoOrigenNuevo,
                    SaldoDestinoNuevo = saldoDestinoNuevo
                });
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: ObtenerUsuarioActual(),
                    accion: "TRANSFERENCIA",
                    resultado: "ERROR",
                    descripcion: $"Error: {ex.Message}",
                    servicio: "CoreBancarioController.Transferir"
                );

                return StatusCode(500, new CoreResponseDto
                {
                    Codigo = -1,
                    Descripcion = "Error interno del servidor"
                });
            }
        }
    }
}