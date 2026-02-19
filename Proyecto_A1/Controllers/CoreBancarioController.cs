using Logica_Negocio.Interfaces;  // ← Cambiado
using Logica_Negocio.Services;    // Para BitacoraService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        // SRV19
        [HttpGet("client-exists")]
        public async Task<IActionResult> ClienteExiste([FromQuery] string identificacion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identificacion))
                {
                    return BadRequest(new { codigo = -1, descripcion = "Identificación requerida" });
                }

                var existe = await _coreService.ClienteExisteAsync(identificacion);

                await _bitacoraService.RegistrarAsync(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_CLIENTE",
                    resultado: "EXITO",
                    descripcion: $"Consulta cliente {identificacion}: {existe}",
                    servicio: "CoreBancarioController.ClienteExiste"
                );

                return Ok(new { codigo = 0, descripcion = "Consulta exitosa", existe });
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAsync(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_CLIENTE",
                    resultado: "ERROR",
                    descripcion: ex.Message,
                    servicio: "CoreBancarioController.ClienteExiste"
                );
                return StatusCode(500, new { codigo = -1, descripcion = "Error interno" });
            }
        }

        // SRV15
        [HttpGet("balance")]
        public async Task<IActionResult> ConsultarSaldo([FromQuery] string identificacion, [FromQuery] string cuenta)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identificacion) || string.IsNullOrWhiteSpace(cuenta))
                {
                    return BadRequest(new { codigo = -1, descripcion = "Identificación y cuenta requeridas" });
                }

                var saldo = await _coreService.ConsultarSaldoAsync(identificacion, cuenta);

                if (saldo == null)
                {
                    return NotFound(new { codigo = -1, descripcion = "Cliente o cuenta no encontrados" });
                }

                await _bitacoraService.RegistrarAsync(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_SALDO",
                    resultado: "EXITO",
                    descripcion: $"Saldo cuenta {cuenta}: {saldo:C}",
                    servicio: "CoreBancarioController.ConsultarSaldo"
                );

                return Ok(new { codigo = 0, descripcion = "Consulta exitosa", saldo });
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAsync(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_SALDO",
                    resultado: "ERROR",
                    descripcion: ex.Message,
                    servicio: "CoreBancarioController.ConsultarSaldo"
                );
                return StatusCode(500, new { codigo = -1, descripcion = "Error interno" });
            }
        }

        // SRV16
        [HttpGet("transactions")]
        public async Task<IActionResult> ObtenerMovimientos([FromQuery] string identificacion, [FromQuery] string cuenta)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identificacion) || string.IsNullOrWhiteSpace(cuenta))
                {
                    return BadRequest(new { codigo = -1, descripcion = "Identificación y cuenta requeridas" });
                }

                var movimientos = await _coreService.ObtenerUltimosMovimientosAsync(identificacion, cuenta);

                await _bitacoraService.RegistrarAsync(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_MOVIMIENTOS",
                    resultado: "EXITO",
                    descripcion: $"Movimientos cuenta {cuenta}: {movimientos.Count}",
                    servicio: "CoreBancarioController.ObtenerMovimientos"
                );

                return Ok(new { codigo = 0, descripcion = "Consulta exitosa", movimientos });
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAsync(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_MOVIMIENTOS",
                    resultado: "ERROR",
                    descripcion: ex.Message,
                    servicio: "CoreBancarioController.ObtenerMovimientos"
                );
                return StatusCode(500, new { codigo = -1, descripcion = "Error interno" });
            }
        }

        // SRV14
        [HttpPost("transaction")]
        public async Task<IActionResult> AplicarTransaccion([FromBody] TransaccionRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Identificacion) ||
                    string.IsNullOrWhiteSpace(request.TipoMovimiento) || request.Monto <= 0)
                {
                    return BadRequest(new { codigo = -1, descripcion = "Datos inválidos" });
                }

                var resultado = await _coreService.AplicarTransaccionAsync(
                    request.Identificacion,
                    request.TipoMovimiento,
                    request.Monto,
                    request.ReferenciaExterna,
                    request.Descripcion
                );

                if (!resultado.Exito)
                {
                    return BadRequest(new { codigo = -1, descripcion = resultado.Mensaje });
                }

                await _bitacoraService.RegistrarAsync(
                    usuario: ObtenerUsuarioActual(),
                    accion: "TRANSACCION",
                    resultado: "EXITO",
                    descripcion: $"{request.TipoMovimiento} {request.Monto:C}",
                    servicio: "CoreBancarioController.AplicarTransaccion"
                );

                return Ok(new { codigo = 0, descripcion = resultado.Mensaje, nuevoSaldo = resultado.NuevoSaldo });
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAsync(
                    usuario: ObtenerUsuarioActual(),
                    accion: "TRANSACCION",
                    resultado: "ERROR",
                    descripcion: ex.Message,
                    servicio: "CoreBancarioController.AplicarTransaccion"
                );
                return StatusCode(500, new { codigo = -1, descripcion = "Error interno" });
            }
        }

        // SRV13
        [HttpGet("accounts/balance")]
        public async Task<IActionResult> ConsultarSaldoPorTelefono([FromQuery] string telefono, [FromQuery] string identificacion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(identificacion))
                {
                    return BadRequest(new { codigo = -1, descripcion = "Teléfono e identificación requeridos" });
                }

                var resultado = await _coreService.ConsultarSaldoPorTelefonoAsync(telefono, identificacion);

                if (!resultado.Exito)
                {
                    return BadRequest(new { codigo = -1, descripcion = resultado.Mensaje });
                }

                await _bitacoraService.RegistrarAsync(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_SALDO_TELEFONO",
                    resultado: "EXITO",
                    descripcion: $"Saldo telf {telefono}: {resultado.Saldo:C}",
                    servicio: "CoreBancarioController.ConsultarSaldoPorTelefono"
                );

                return Ok(new { codigo = 0, descripcion = resultado.Mensaje, saldo = resultado.Saldo });
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAsync(
                    usuario: ObtenerUsuarioActual(),
                    accion: "CONSULTA_SALDO_TELEFONO",
                    resultado: "ERROR",
                    descripcion: ex.Message,
                    servicio: "CoreBancarioController.ConsultarSaldoPorTelefono"
                );
                return StatusCode(500, new { codigo = -1, descripcion = "Error interno" });
            }
        }
    }

    public class TransaccionRequest
    {
        public string Identificacion { get; set; } = null!;
        public string TipoMovimiento { get; set; } = null!;
        public decimal Monto { get; set; }
        public string? ReferenciaExterna { get; set; }
        public string? Descripcion { get; set; }
    }
}