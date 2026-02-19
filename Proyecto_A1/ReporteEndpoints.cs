using Entities.DTOs;
using Logica_Negocio.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_A1.Endpoints
{
    public static class ReporteEndpoints
    {
        public static void MapReporteEndpoints(this IEndpointRouteBuilder app)
        {
            // SRV17 - Generar reporte diario (POST)
            app.MapPost("/reportes/diario", GenerarReporteDiario)
                .WithName("GenerarReporteDiario")
                .WithOpenApi()
                .Produces<ReporteDiarioResponse>(StatusCodes.Status200OK)
                .Produces<ReporteDiarioResponse>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status500InternalServerError);
        }

        internal static async Task<IResult> GenerarReporteDiario(
            [FromBody] ReporteDiarioRequest request,
            HttpContext httpContext,
            [FromServices] ReporteService reporteService,
            [FromServices] ILogger<Program> logger)
        {
            logger.LogInformation("SRV17 - POST /reportes/diario llamado");

            // Extraer token del header (puede ser null)
            var token = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var tokenParaUsar = token ?? "sistema-token-123456";

            var result = await reporteService.GenerarReporteDiarioAsync(request, tokenParaUsar);

            // 👇 CORREGIDO: Pattern matching para acceder a las propiedades
            if (result is ReporteDiarioResponse reporte)
            {
                if (reporte.Codigo == 0)
                {
                    return Results.Ok(reporte);
                }
                else if (reporte.Descripcion != null && reporte.Descripcion.Contains("Token"))
                {
                    return Results.Unauthorized();
                }
                else
                {
                    return Results.BadRequest(reporte);
                }
            }

            // Si algo sale mal, retornar error genérico
            return Results.BadRequest(new ReporteDiarioResponse
            {
                Codigo = -1,
                Descripcion = "Error inesperado en el servidor"
            });
        }
    }
}