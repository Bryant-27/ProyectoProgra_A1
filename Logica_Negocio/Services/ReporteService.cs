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
        private readonly BitacoraService _bitacoraService;

        public ReporteService(
            ILogger<ReporteService> logger,
            TransaccionRepository transaccionRepository,
            BitacoraService bitacoraService)      

        {
            _logger = logger;
            _transaccionRepository = transaccionRepository;
             _bitacoraService = bitacoraService;

        }

        /// Genera reporte de transacciones diarias (SRV17)   
        public async Task<ReporteDiarioResponse> GenerarReporteDiarioAsync(ReporteDiarioRequest request, string token)
        {
            try
            {
                _logger.LogInformation("SRV17 - Generando reporte diario para fecha: {Fecha}", request?.Fecha);

                if (string.IsNullOrEmpty(token) || token.Length < 10)
                {
                    return new ReporteDiarioResponse
                    {
                        Codigo = -1,
                        Descripcion = "Token inválido o no autorizado"
                    };
                }

                if (request == null || request.Fecha == DateTime.MinValue)
                {
                    return new ReporteDiarioResponse
                    {
                        Codigo = -1,
                        Descripcion = "Debe enviar los datos completos"
                    };
                }

                var fechaInicio = request.Fecha.Date;
                var fechaFin = fechaInicio.AddDays(1).AddTicks(-1);

                var transacciones = await _transaccionRepository.ObtenerPorFechaYEntidadAsync(
                fechaInicio,
                fechaFin,
                request.EntidadId);

                if (transacciones == null || !transacciones.Any())
                {
                    return new ReporteDiarioResponse
                    {
                        Codigo = 0,
                        Descripcion = "No se encontraron transacciones",
                        TotalTransacciones = 0,
                        MontoTotal = 0
                    };
                }

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

                return new ReporteDiarioResponse
                {
                    Codigo = 0,
                    Descripcion = "Reporte generado exitosamente",
                    Fecha = request.Fecha,
                    TotalTransacciones = transacciones.Count,
                    MontoTotal = montoTotal,
                    Transacciones = transaccionesDto
                };
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