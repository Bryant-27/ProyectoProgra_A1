using Logica_Negocio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataAccess.Models;
using Entities.DTOs;  // ← Usar DTOs de Entities
using System.Security.Claims;

namespace Proyecto_A1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BitacoraController : ControllerBase
    {
        private readonly IBitacoraConsultaService _bitacoraConsultaService;
        private readonly IBitacoraService _bitacoraService;

        public BitacoraController(
            IBitacoraConsultaService bitacoraConsultaService,
            IBitacoraService bitacoraService)
        {
            _bitacoraConsultaService = bitacoraConsultaService;
            _bitacoraService = bitacoraService;
        }

        private string ObtenerUsuarioActual()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? "Sistema";
        }

        // GET /api/bitacora
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateTime? fechaDesde,
            [FromQuery] DateTime? fechaHasta,
            [FromQuery] string usuario,
            [FromQuery] string accion,
            [FromQuery] string servicio,
            [FromQuery] string resultado,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanoPagina = 50)
        {
            try
            {
                var filtros = new BitacoraFiltrosDto  // ← Ahora usa Entities.DTOs
                {
                    FechaDesde = fechaDesde,
                    FechaHasta = fechaHasta,
                    Usuario = usuario,
                    Accion = accion,
                    Servicio = servicio,
                    Resultado = resultado
                };

                var resultadoConsulta = await _bitacoraConsultaService.ObtenerBitacorasAsync(filtros, pagina, tamanoPagina);


                return Ok(new
                {
                    codigo = 0,
                    descripcion = "Consulta exitosa",
                    totalRegistros = resultadoConsulta.TotalRegistros,
                    paginaActual = pagina,
                    tamanoPagina = tamanoPagina,
                    totalPaginas = (int)Math.Ceiling((double)resultadoConsulta.TotalRegistros / tamanoPagina),
                    datos = resultadoConsulta.Bitacoras
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    codigo = -1,
                    descripcion = $"Error al consultar bitácoras: {ex.Message}"
                });
            }
        }

        // GET /api/bitacora/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var bitacora = await _bitacoraConsultaService.ObtenerPorIdAsync(id);

                if (bitacora == null)
                {
                    return NotFound(new
                    {
                        codigo = -1,
                        descripcion = "Bitácora no encontrada"
                    });
                }

                return Ok(new
                {
                    codigo = 0,
                    descripcion = "Consulta exitosa",
                    datos = bitacora
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    codigo = -1,
                    descripcion = $"Error: {ex.Message}"
                });
            }
        }

        // GET /api/bitacora/usuario/{usuario}
        [HttpGet("usuario/{usuario}")]
        public async Task<IActionResult> GetByUsuario(
            string usuario,
            [FromQuery] DateTime? fechaDesde,
            [FromQuery] DateTime? fechaHasta)
        {
            try
            {
                var filtros = new BitacoraFiltrosDto
                {
                    Usuario = usuario,
                    FechaDesde = fechaDesde,
                    FechaHasta = fechaHasta
                };

                var resultado = await _bitacoraConsultaService.ObtenerBitacorasAsync(filtros, 1, 1000);

                return Ok(new
                {
                    codigo = 0,
                    descripcion = "Consulta exitosa",
                    datos = resultado.Bitacoras
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    codigo = -1,
                    descripcion = $"Error: {ex.Message}"
                });
            }
        }

        // GET /api/bitacora/accion/{accion}
        [HttpGet("accion/{accion}")]
        public async Task<IActionResult> GetByAccion(string accion)
        {
            try
            {
                var filtros = new BitacoraFiltrosDto { Accion = accion };
                var resultado = await _bitacoraConsultaService.ObtenerBitacorasAsync(filtros, 1, 1000);

                return Ok(new
                {
                    codigo = 0,
                    descripcion = "Consulta exitosa",
                    datos = resultado.Bitacoras
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    codigo = -1,
                    descripcion = $"Error: {ex.Message}"
                });
            }
        }

        // GET /api/bitacora/servicio/{servicio}
        [HttpGet("servicio/{servicio}")]
        public async Task<IActionResult> GetByServicio(string servicio)
        {
            try
            {
                var filtros = new BitacoraFiltrosDto { Servicio = servicio };
                var resultado = await _bitacoraConsultaService.ObtenerBitacorasAsync(filtros, 1, 1000);

                return Ok(new
                {
                    codigo = 0,
                    descripcion = "Consulta exitosa",
                    datos = resultado.Bitacoras
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    codigo = -1,
                    descripcion = $"Error: {ex.Message}"
                });
            }
        }

        // GET /api/bitacora/fecha
        [HttpGet("fecha")]
        public async Task<IActionResult> GetByFecha(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta)
        {
            try
            {
                if (desde > hasta)
                {
                    return BadRequest(new
                    {
                        codigo = -1,
                        descripcion = "La fecha 'desde' no puede ser mayor que 'hasta'"
                    });
                }

                var filtros = new BitacoraFiltrosDto
                {
                    FechaDesde = desde,
                    FechaHasta = hasta
                };

                var resultado = await _bitacoraConsultaService.ObtenerBitacorasAsync(filtros, 1, 1000);

                return Ok(new
                {
                    codigo = 0,
                    descripcion = "Consulta exitosa",
                    datos = resultado.Bitacoras
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    codigo = -1,
                    descripcion = $"Error: {ex.Message}"
                });
            }
        }

        // GET /api/bitacora/estadisticas
        [HttpGet("estadisticas")]
        public async Task<IActionResult> GetEstadisticas([FromQuery] DateTime? fechaDesde, [FromQuery] DateTime? fechaHasta)
        {
            try
            {
                var estadisticas = await _bitacoraConsultaService.ObtenerEstadisticasAsync(fechaDesde, fechaHasta);

                return Ok(new
                {
                    codigo = 0,
                    descripcion = "Consulta exitosa",
                    datos = estadisticas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    codigo = -1,
                    descripcion = $"Error: {ex.Message}"
                });
            }
        }
    }
}