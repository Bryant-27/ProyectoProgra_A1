using Logica_Negocio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entities.DTOs;
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
        [HttpGet("client-exists")]
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

                await _bitacoraService.RegistrarAsync(
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
                await _bitacoraService.RegistrarAsync(
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
        [HttpGet("balance")]
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

                await _bitacoraService.RegistrarAsync(
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
                await _bitacoraService.RegistrarAsync(
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
        [HttpGet("transactions")]
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
                    SaldoAnterior = (decimal)m.SaldoAnterior,
                    SaldoNuevo = (decimal)m.SaldoNuevo
                }).ToList();

                await _bitacoraService.RegistrarAsync(
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
                await _bitacoraService.RegistrarAsync(
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

        // SRV14: Aplicar transacción
        [HttpPost("transaction")]
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

            
                var resultado = await _coreService.AplicarTransaccionAsync(
                    request.Identificacion,
                    request.TipoMovimiento,
                    (decimal)request.Monto,         
                    request.ReferenciaExterna,
                    request.Descripcion
                );

                if (!resultado.Exito)
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = resultado.Mensaje
                    });
                }

                await _bitacoraService.RegistrarAsync(
                    usuario: ObtenerUsuarioActual(),
                    accion: "TRANSACCION",
                    resultado: "EXITO",
                    descripcion: $"{request.TipoMovimiento} por {request.Monto:C} aplicado. Ref: {request.ReferenciaExterna}",
                    servicio: "CoreBancarioController.AplicarTransaccion"
                );

                return Ok(new
                {
                    codigo = 0,
                    descripcion = resultado.Mensaje,
                    nuevoSaldo = resultado.NuevoSaldo
                });
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAsync(
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

        // SRV13: Consultar saldo por teléfono
        [HttpGet("accounts/balance")]
        public async Task<IActionResult> ConsultarSaldoPorTelefono([FromQuery] string telefono, [FromQuery] string identificacion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(identificacion))
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = "Debe enviar los datos completos y válidos"
                    });
                }

                var resultado = await _coreService.ConsultarSaldoPorTelefonoAsync(telefono, identificacion);

                if (!resultado.Exito)
                {
                    return BadRequest(new CoreResponseDto
                    {
                        Codigo = -1,
                        Descripcion = resultado.Mensaje
                    });
                }

                await _bitacoraService.RegistrarAsync(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_SALDO_TELEFONO",
                    resultado: "EXITO",
                    descripcion: $"Saldo consultado para teléfono {telefono}: {resultado.Saldo:C}",
                    servicio: "CoreBancarioController.ConsultarSaldoPorTelefono"
                );

                return Ok(new SaldoResponseDto
                {
                    Codigo = 0,
                    Descripcion = resultado.Mensaje,
                    Saldo = resultado.Saldo
                });
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAsync(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_SALDO_TELEFONO",
                    resultado: "ERROR",
                    descripcion: $"Error: {ex.Message}",
                    servicio: "CoreBancarioController.ConsultarSaldoPorTelefono"
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