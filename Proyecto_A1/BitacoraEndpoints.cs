using Entities.DTOs;
using Logica_Negocio.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Proyecto_A1
{
    public static class BitacoraEndpoints
    {
        public static void MapBitacoraEndpoints(this IEndpointRouteBuilder app)
        {
            // SRV18 - Registrar bitácora (POST)
            app.MapPost("/bitacora", RegistrarBitacora)
                .WithName("RegistrarBitacora")
                .WithOpenApi()
                .Produces<BitacoraResponse>(StatusCodes.Status201Created)
                .Produces<BitacoraResponse>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status500InternalServerError);

            // SRV18 - Consultar bitácoras (GET)
            app.MapGet("/bitacora", ConsultarBitacoras)
                .WithName("ConsultarBitacoras")
                .WithOpenApi()
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status500InternalServerError);
        }

        // POST /bitacora - Registrar nueva bitácora
        internal static async Task<IResult> RegistrarBitacora(
            [FromBody] BitacoraRequest request,
            HttpContext httpContext,
            [FromServices] BitacoraService bitacoraService,
            [FromServices] ILogger<Program> logger)
        {
            logger.LogInformation("SRV18 - POST /bitacora llamado");

            // Extraer token del header
            var token = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            var result = await bitacoraService.RegistrarAsync(request, token);

            if (result.Codigo == 0)
            {
                return Results.Created($"/bitacora/{result.BitacoraId}", result);
            }
            else if (result.Descripcion.Contains("Token"))
            {
                return Results.Unauthorized();
            }
            else
            {
                return Results.BadRequest(result);
            }
        }

        // GET /bitacora - Consultar bitácoras
        internal static async Task<IResult> ConsultarBitacoras(
            HttpContext httpContext,
            [FromServices] BitacoraService bitacoraService,
            [FromServices] ILogger<Program> logger,
            [FromQuery] string? usuario = null,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] string? accion = null,
            [FromQuery] string? resultado = null)
        {
            logger.LogInformation("SRV18 - GET /bitacora llamado");

            // Extraer token del header
            var token = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            var result = await bitacoraService.ConsultarAsync(
                usuario, fechaInicio, fechaFin, accion, resultado, token);

            // Verificar si es una respuesta de error
            if (result is BitacoraResponse errorResponse && errorResponse.Codigo == -1)
            {
                if (errorResponse.Descripcion.Contains("Token"))
                {
                    return Results.Unauthorized();
                }
                return Results.BadRequest(errorResponse);
            }

            return Results.Ok(result);
        }
    }
}