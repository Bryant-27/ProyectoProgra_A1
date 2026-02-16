using System.Collections.Generic;
using Entities.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DataAccess.Repositories;
using DataAccess.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Logica_Negocio.Services   
{
    public class BitacoraService
    {
        private readonly BitacoraRepository _bitacoraRepository;
        private readonly ILogger<BitacoraService> _logger;

        public BitacoraService(
            BitacoraRepository bitacoraRepository,
            ILogger<BitacoraService> logger)
        {
            _bitacoraRepository = bitacoraRepository;
            _logger = logger;
        }

        // Registrar una nueva bitácora
        public async Task<BitacoraResponse> RegistrarAsync(BitacoraRequest request, string token)
        {
            try
            {
                _logger.LogInformation("SRV18 - Registrando bitácora para usuario: {Usuario}", request?.Usuario);

                // Validar token (simulado)
                if (string.IsNullOrEmpty(token) || token.Length < 10)
                {
                    return new BitacoraResponse
                    {
                        Codigo = -1,
                        Descripcion = "Token inválido o no autorizado"
                    };
                }

                // Validar request
                if (request == null)
                {
                    return new BitacoraResponse
                    {
                        Codigo = -1,
                        Descripcion = "Debe enviar los datos completos"
                    };
                }

                // Validar campos requeridos
                if (string.IsNullOrWhiteSpace(request.Usuario) ||
                    string.IsNullOrWhiteSpace(request.Accion) ||
                    string.IsNullOrWhiteSpace(request.Descripcion))
                {
                    return new BitacoraResponse
                    {
                        Codigo = -1,
                        Descripcion = "Todos los datos son requeridos y no pueden estar vacíos"
                    };
                }

                // Crear entidad Bitacora
                var bitacora = new Bitacora
                {
                    Usuario = request.Usuario.Trim(),
                    Accion = request.Aaccion.Trim(),
                    Descripcion = request.Descripcion.Trim(),
                    Servicio = "SRV18",
                    Resultado = "INFO",
                    FechaRegistro = DateTime.Now
                };

                // Guardar en base de datos
                var resultado = await _bitacoraRepository.RegistrarAsync(bitacora);

                _logger.LogInformation("SRV18 - Bitácora registrada exitosamente ID: {Id}", resultado.Id);

                return new BitacoraResponse
                {
                    Codigo = 0,
                    Descripcion = "Bitácora registrada exitosamente",
                    BitacoraId = resultado.Id,
                    FechaRegistro = resultado.FechaRegistro
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SRV18 - Error registrando bitácora");
                return new BitacoraResponse
                {
                    Codigo = -1,
                    Descripcion = "Error interno del servidor"
                };
            }
        }

        // Consultar bitácoras
        public async Task<object> ConsultarAsync(
            string usuario,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            string accion,
            string resultado,
            string token)
        {
            try
            {
                _logger.LogInformation("SRV18 - Consultando bitácoras");

                // Validar token
                if (string.IsNullOrEmpty(token) || token.Length < 10)
                {
                    return new BitacoraResponse
                    {
                        Codigo = -1,
                        Descripcion = "Token inválido o no autorizado"
                    };
                }

                // Obtener bitácoras
                var bitacoras = await _bitacoraRepository.ListarAsync(
                    usuario, fechaInicio, fechaFin, accion, resultado);

                if (bitacoras == null || !bitacoras.Any())
                {
                    return new
                    {
                        codigo = 0,
                        descripcion = "No se encontraron bitácoras",
                        bitacoras = new List<BitacoraConsultaResponse>()
                    };
                }

                // Mapear a DTO de respuesta
                var respuesta = bitacoras.Select(b => new BitacoraConsultaResponse
                {
                    BitacoraId = b.Id,
                    Usuario = b.Usuario,
                    Accion = b.Accion,
                    Descripcion = b.Descripcion,
                    FechaRegistro = b.FechaRegistro,
                    Servicio = b.Servicio,
                    Resultado = b.Resultado
                }).ToList();

                return new
                {
                    codigo = 0,
                    descripcion = "Bitácoras encontradas",
                    total = respuesta.Count,
                    bitacoras = respuesta
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SRV18 - Error consultando bitácoras");
                return new BitacoraResponse
                {
                    Codigo = -1,
                    Descripcion = "Error interno del servidor"
                };
            }
        }
    }
}