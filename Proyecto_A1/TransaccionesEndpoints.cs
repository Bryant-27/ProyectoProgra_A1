using Logica_Negocio.Services;
using Microsoft.AspNetCore.Mvc;

namespace Proyecto_A1
{
    public static class TransaccionesEndpoints
    {
        public static void MapTransaccionesEndpoints(this WebApplication app)
        {
            // HU SRV7: Recibir transacciones (NO requiere autenticación)
            app.MapPost("/api/transactions/process", async (
                [FromBody] TransaccionService.TransaccionRequest request,
                [FromServices] TransaccionService transaccionService) =>
            {
                if (request == null)
                    return Results.BadRequest(new { codigo = -1, descripcion = "Request inválido" });

                var response = await transaccionService.RecibirTransaccionAsync(request);

                if (response.Codigo == 0)
                    return Results.Ok(response);
                else
                    return Results.BadRequest(response);
            })
            .WithName("SRV7_RecibirTransaccion")
            .Produces<TransaccionService.TransaccionResponse>(200)
            .Produces<TransaccionService.TransaccionResponse>(400)
            .AllowAnonymous();

            // HU SRV8: Enviar transacciones (REQUIERE autenticación)
            app.MapPost("/api/transactions/send", async (
                HttpContext httpContext,
                [FromBody] TransaccionService.TransaccionRequest request,
                [FromServices] TransaccionService transaccionService) =>
            {
                if (request == null)
                    return Results.BadRequest(new { codigo = -1, descripcion = "Request inválido" });

                // Obtener token del header Authorization
                var authHeader = httpContext.Request.Headers["Authorization"].ToString();
                string token = "";

                if (authHeader.StartsWith("Bearer "))
                    token = authHeader.Substring("Bearer ".Length);

                var response = await transaccionService.EnviarTransaccionAsync(request, token);

                if (response.Codigo == 0)
                    return Results.Ok(response);
                else if (response.Descripcion.Contains("No autorizado"))
                    return Results.Unauthorized();
                else
                    return Results.BadRequest(response);
            })
            .WithName("SRV8_EnviarTransaccion")
            .Produces<TransaccionService.TransaccionResponse>(200)
            .Produces(401)
            .Produces<TransaccionService.TransaccionResponse>(400)
            .RequireAuthorization();

            // HU SRV12: Resolver transacciones (REQUIERE autenticación)
            app.MapPost("/api/transactions/route", async (
                HttpContext httpContext,
                [FromBody] TransaccionService.TransaccionRequest request,
                [FromServices] TransaccionService transaccionService,
                [FromServices] AuthService authService) =>
            {
                if (request == null)
                    return Results.BadRequest(new { codigo = -1, descripcion = "Request inválido" });

                // Obtener token
                var authHeader = httpContext.Request.Headers["Authorization"].ToString();
                string token = "";

                if (authHeader.StartsWith("Bearer "))
                    token = authHeader.Substring("Bearer ".Length);

                // Validar token
                if (string.IsNullOrEmpty(token) || !await authService.ValidateTokenAsync(token))
                    return Results.Unauthorized();

                var response = await transaccionService.RecibirTransaccionAsync(request);

                if (response.Codigo == 0)
                    return Results.Ok(response);
                else
                    return Results.BadRequest(response);
            })
            .WithName("SRV12_ResolverTransaccion")
            .Produces<TransaccionService.TransaccionResponse>(200)
            .Produces(401)
            .Produces<TransaccionService.TransaccionResponse>(400)
            .RequireAuthorization();

            // HU SRV17: Reporte de transacciones diarias (REQUIERE autenticación)
            app.MapGet("/api/reports/daily", async (
                HttpContext httpContext,
                [FromQuery] DateTime? fecha,
                [FromServices] TransaccionService transaccionService,
                [FromServices] AuthService authService) =>
            {
                // Obtener token
                var authHeader = httpContext.Request.Headers["Authorization"].ToString();
                string token = "";

                if (authHeader.StartsWith("Bearer "))
                    token = authHeader.Substring("Bearer ".Length);

                // Validar token
                if (string.IsNullOrEmpty(token) || !await authService.ValidateTokenAsync(token))
                    return Results.Unauthorized();

                // Usar fecha actual si no se especifica
                var fechaReporte = fecha?.Date ?? DateTime.Today;

                try
                {
                    var reporte = await transaccionService.GenerarReporteDiarioAsync(fechaReporte, token);
                    return Results.Ok(reporte);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Error al generar reporte",
                        detail: ex.Message,
                        statusCode: 500);
                }
            })
            .WithName("SRV17_ReporteDiario")
            .Produces<TransaccionService.ReporteDiarioResponse>(200)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization();

            // Login endpoint
            app.MapPost("/api/login", async (
                [FromBody] LoginRequest loginRequest,
                [FromServices] AuthService authService) =>
            {
                if (string.IsNullOrEmpty(loginRequest.Usuario) || string.IsNullOrEmpty(loginRequest.Contrasena))
                    return Results.BadRequest(new { error = "Usuario y contraseña son requeridos" });

                var result = await authService.LoginAsync(loginRequest.Usuario, loginRequest.Contrasena);

                if (result.Success)
                {
                    return Results.Ok(new
                    {
                        access_token = result.Token,
                        expires_in = 300,
                        refresh_token = Guid.NewGuid().ToString(),
                        usuarioID = result.UsuarioId
                    });
                }

                return Results.Unauthorized();
            })
            .WithName("Login")
            .AllowAnonymous();

            // Endpoint para validar token
            app.MapGet("/api/validate", async (
                [FromQuery] string token,
                [FromServices] AuthService authService) =>
            {
                if (string.IsNullOrEmpty(token))
                    return Results.BadRequest(new { valid = false, message = "Token requerido" });

                var isValid = await authService.ValidateTokenAsync(token);

                if (isValid)
                    return Results.Ok(new { valid = true });
                else
                    return Results.Unauthorized();
            })
            .WithName("ValidateToken")
            .AllowAnonymous();
        }
    }

    // DTO para login
    public class LoginRequest
    {
        public string Usuario { get; set; } = null!;
        public string Contrasena { get; set; } = null!;
    }
}