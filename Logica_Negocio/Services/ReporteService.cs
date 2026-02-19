using Entities.DTOs;
using Microsoft.Extensions.Logging;
using DataAccess.Repositories;
using DataAccess.Models;
using Logica_Negocio.Interfaces;  // ← AGREGADO: Para IBitacoraService
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
        private readonly IBitacoraService _bitacoraService;  // ← CAMBIADO: Usa interfaz en lugar de implementación concreta

        public ReporteService(
            ILogger<ReporteService> logger,
            TransaccionRepository transaccionRepository,
            IBitacoraService bitacoraService)  // ← CAMBIADO: Ahora recibe la interfaz
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
                    // Registrar intento fallido en bitácora
                    await _bitacoraService.RegistrarAsync(
                        usuario: "Sistema",
                        accion: "REPORTE_DIARIO",
                        resultado: "NO_AUTORIZADO",
                        descripcion: "Token inválido",
                        servicio: "ReporteService.GenerarReporteDiarioAsync"
                    );

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
                    // Registrar en bitácora que no hay transacciones
                    await _bitacoraService.RegistrarAsync(
                        usuario: "Sistema",
                        accion: "REPORTE_DIARIO",
                        resultado: "SIN_DATOS",
                        descripcion: $"No se encontraron transacciones para fecha {request.Fecha:yyyy-MM-dd}",
                        servicio: "ReporteService.GenerarReporteDiarioAsync"
                    );

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

                // Registrar éxito en bitácora
                await _bitacoraService.RegistrarAsync(
                    usuario: "Sistema",
                    accion: "REPORTE_DIARIO",
                    resultado: "EXITO",
                    descripcion: $"Reporte generado: {transacciones.Count} transacciones, total {montoTotal:C}",
                    servicio: "ReporteService.GenerarReporteDiarioAsync"
                );

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

                // Registrar error en bitácora
                await _bitacoraService.RegistrarAsync(
                    usuario: "Sistema",
                    accion: "REPORTE_DIARIO",
                    resultado: "ERROR",
                    descripcion: ex.Message,
                    servicio: "ReporteService.GenerarReporteDiarioAsync"
                );

                return new ReporteDiarioResponse
                {
                    Codigo = -1,
                    Descripcion = "Error interno del servidor"
                };
            }
        }
    }
}