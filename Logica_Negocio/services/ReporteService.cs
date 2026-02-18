using Entities.DTOs;
using Microsoft.Extensions.Logging;
using DataAccess.Repositories;
using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logica_Negocio.Services
{
    public class ReporteService
    {
        private readonly ILogger<ReporteService> _logger;
        private readonly TransaccionRepository _transaccionRepository;
        //private readonly BitacoraService _bitacoraService;

        public ReporteService(
            ILogger<ReporteService> logger,
            TransaccionRepository transaccionRepository)
            //BitacoraService bitacoraService)      

        {
            _logger = logger;
            _transaccionRepository = transaccionRepository;

        }

        /// Genera reporte de transacciones diarias (SRV17)   
        public async Task<ReporteDiarioResponse> GenerarReporteDiarioAsync(ReporteDiarioRequest request, string token)
        {
            try
            {
                _logger.LogInformation("SRV17 - Generando reporte diario para fecha: {Fecha}", request?.Fecha ?? DateTime.MinValue);

                #region Validar token
                if (string.IsNullOrEmpty(token) || token.Length < 10)
                {
                    _logger.LogWarning("SRV17 - Token inválido");
                    return new ReporteDiarioResponse
                    {
                        Codigo = -1,
                        Descripcion = "Token inválido o no autorizado"
                    };
                }
                #endregion

                #region Validar request
                if (request == null)
                {
                    _logger.LogWarning("SRV17 - Request nulo");
                    return new ReporteDiarioResponse
                    {
                        Codigo = -1,
                        Descripcion = "Debe enviar los datos completos"
                    };
                }

                if (request.Fecha == DateTime.MinValue)
                {
                    _logger.LogWarning("SRV17 - Fecha inválida");
                    return new ReporteDiarioResponse
                    {
                        Codigo = -1,
                        Descripcion = "La fecha es requerida"
                    };
                }
                #endregion

                #region Obtener transacciones del día
                var fechaInicio = request.Fecha.Date;
                var fechaFin = fechaInicio.AddDays(1).AddTicks(-1);

                var transacciones = await _transaccionRepository.ObtenerPorFechaYEntidadAsync(
                    fechaInicio,
                    fechaFin,
                    request.EntidadId);

                if (transacciones == null || !transacciones.Any())
                {
                    _logger.LogInformation("SRV17 - No se encontraron transacciones para {Fecha}", fechaInicio.ToString("yyyy-MM-dd"));

                    return new ReporteDiarioResponse
                    {
                        Codigo = 0,
                        Descripcion = "No se encontraron transacciones para la fecha indicada",
                        Fecha = request.Fecha,
                        TotalTransacciones = 0,
                        MontoTotal = 0,
                        Transacciones = new List<TransaccionReporteDto>() 
                    };
                }
                #endregion

                #region Procesar y armar reporte
                var montoTotal = transacciones.Sum(t => t.Monto);

                var transaccionesDto = transacciones.Select(t => new TransaccionReporteDto
                {
                    Id = t.IdTransaccion,                    
                    TelefonoOrigen = t.TelefonoOrigen,      
                    NombreOrigen = t.NombreOrigen,          
                    TelefonoDestino = t.TelefonoDestino,     
                    Monto = t.Monto,
                    Fecha = t.FechaEnvio ?? DateTime.Now,    
                    Estado = t.IdEstado == 4 ? "COMPLETADA" : "PENDIENTE" 
                }).ToList();

                var reporte = new ReporteDiarioResponse
                {
                    Codigo = 0,
                    Descripcion = "Reporte generado exitosamente",
                    Fecha = request.Fecha,
                    TotalTransacciones = transacciones.Count,
                    MontoTotal = montoTotal,
                    Transacciones = transaccionesDto
                };

                var reportegenerado = new ReporteDiarioResponse
                {
                    Codigo = 0,
                    Descripcion = "Reporte generado exitosamente",
                    Fecha = request.Fecha,
                    TotalTransacciones = transacciones.Count,
                    MontoTotal = montoTotal,
                    Transacciones = transaccionesDto
                };

                _logger.LogInformation("SRV17 - Reporte generado: {Count} transacciones, Monto: ₡{Monto:N2}",
                    transacciones.Count, montoTotal);

                return reporte;
                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SRV17 - Error generando reporte diario");

                return new ReporteDiarioResponse
                {
                    Codigo = -1,
                    Descripcion = "Error interno del servidor"
                };
            }
        }
    }
}