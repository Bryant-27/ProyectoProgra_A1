using Entities.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Logica_Negocio.Services;

namespace Proyecto_A1.Endpoints
{
    public static class TransaccionesEndpoints
    {
        public static void MapTransaccionesEndpoints(this IEndpointRouteBuilder app)
        {
            // SRV7 - Endpoint público para RECIBIR transacciones
            app.MapPost("/transactions/process", ProcessTransaction)
                .WithName("ProcessTransaction")
                .WithOpenApi()
                .Produces<TransaccionResponse>(StatusCodes.Status200OK)
                .Produces<TransaccionResponse>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status500InternalServerError)
                .AllowAnonymous();

            // SRV8 - Endpoint para ENVIAR transacciones a entidades externas
            app.MapPost("/transactions/send", SendTransaction)
                .WithName("SendTransaction")
                .WithOpenApi()
                .Produces<TransaccionResponse>(StatusCodes.Status200OK)
                .Produces<TransaccionResponse>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status500InternalServerError);

            // SRV12 - Endpoint para RUTEAR transacciones
            app.MapPost("/transactions/route", RouteTransaction)
                .WithName("RouteTransaction")
                .WithOpenApi()
                .Produces<TransaccionResponse>(StatusCodes.Status200OK)
                .Produces<TransaccionResponse>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status500InternalServerError);

            // SRV17 - Endpoint para REPORTE DIARIO
            app.MapPost("/reportes/diario", GenerarReporteDiario)
                .WithName("GenerarReporteDiario")
                .WithOpenApi()
                .Produces<ReporteDiarioResponse>(StatusCodes.Status200OK)
                .Produces<ReporteDiarioResponse>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status500InternalServerError);
        }

        // ==================== SRV7 HANDLER ====================
        internal static async Task<IResult> ProcessTransaction(
            [FromBody] TransaccionRequest request,
            HttpContext httpContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            [FromServices] ILogger<Program> logger)
        {
            logger.LogInformation("==========================================");
            logger.LogInformation("SRV7 - Recibiendo transacción");
            logger.LogInformation("De: {TelefonoOrigen} - {NombreOrigen}", request?.TelefonoOrigen, request?.NombreOrigen);
            logger.LogInformation("Para: {TelefonoDestino}", request?.TelefonoDestino);
            logger.LogInformation("Monto: ₡{Monto:N2}", request?.Monto);
            logger.LogInformation("==========================================");

            // Validar request nulo
            if (request == null)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Debe enviar los datos completos y válidos"
                });
            }

            // Validar campos requeridos
            if (string.IsNullOrWhiteSpace(request.TelefonoOrigen) ||
                string.IsNullOrWhiteSpace(request.NombreOrigen) ||
                string.IsNullOrWhiteSpace(request.TelefonoDestino) ||
                string.IsNullOrWhiteSpace(request.Descripcion))
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Debe enviar los datos completos y válidos"
                });
            }

            // Validar teléfono origen
            if (request.TelefonoOrigen.Length != 8 || !request.TelefonoOrigen.All(char.IsDigit))
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El teléfono origen debe tener 8 dígitos numéricos"
                });
            }

            // Validar teléfono destino
            if (request.TelefonoDestino.Length != 8 || !request.TelefonoDestino.All(char.IsDigit))
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El teléfono destino debe tener 8 dígitos numéricos"
                });
            }

            // Validar descripción
            if (request.Descripcion.Length > 25)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "La descripción no puede superar 25 caracteres"
                });
            }

            // Validar monto
            if (request.Monto <= 0)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El monto debe ser mayor a cero"
                });
            }

            if (request.Monto > 100000)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El monto no debe ser superior a 100.000"
                });
            }

            // Validar entidad destino
            var grupoEntidadId = int.Parse(config["AppSettings:GrupoEntidadId"] ?? "1");
            if (request.EntidadDestino != grupoEntidadId)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "La entidad destino no es válida"
                });
            }

            // Validar entidad origen (simulado)
            if (request.EntidadOrigen != 1)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "La entidad origen no está registrada o no está activa"
                });
            }

            try
            {
                // Llamar a SRV12 para enrutar
                var routingServiceUrl = config["Services:RoutingService"] ?? "https://localhost:7001";
                logger.LogInformation("SRV7 - Llamando a SRV12: {Url}/transactions/route", routingServiceUrl);

                var httpClient = httpClientFactory.CreateClient();

                var routingRequest = new
                {
                    TelefonoOrigen = request.TelefonoOrigen,
                    NombreOrigen = request.NombreOrigen,
                    TelefonoDestino = request.TelefonoDestino,
                    Monto = request.Monto,
                    Descripcion = request.Descripcion
                };

                var response = await httpClient.PostAsJsonAsync($"{routingServiceUrl}/transactions/route", routingRequest);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TransaccionResponse>();

                    if (result != null && result.Codigo == 0)
                    {
                        logger.LogInformation("SRV7 - Transacción procesada exitosamente");
                        return Results.Ok(new TransaccionResponse
                        {
                            Codigo = 0,
                            Descripcion = "Transacción aplicada"
                        });
                    }
                    else
                    {
                        logger.LogWarning("SRV7 - SRV12 respondió con error: {Descripcion}", result?.Descripcion);
                        return Results.BadRequest(new TransaccionResponse
                        {
                            Codigo = -1,
                            Descripcion = result?.Descripcion ?? "Error en servicio de enrutamiento"
                        });
                    }
                }
                else
                {
                    logger.LogError("SRV7 - SRV12 respondió con status: {StatusCode}", response.StatusCode);
                    return Results.BadRequest(new TransaccionResponse
                    {
                        Codigo = -1,
                        Descripcion = "Error al procesar la transacción"
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SRV7 - Error llamando a SRV12");
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Error de comunicación con servicio interno"
                });
            }
        }

        // ==================== SRV8 HANDLER ====================
        internal static async Task<IResult> SendTransaction(
            [FromBody] EnvioTransaccionRequest request,
            HttpContext httpContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            [FromServices] ILogger<Program> logger)
        {
            logger.LogInformation("==========================================");
            logger.LogInformation("SRV8 - Enviando transacción a entidad externa");
            logger.LogInformation("Entidad Origen: {EntidadOrigen}", request?.EntidadOrigen);
            logger.LogInformation("Teléfono Origen: {TelefonoOrigen}", request?.TelefonoOrigen);
            logger.LogInformation("Nombre Origen: {NombreOrigen}", request?.NombreOrigen);
            logger.LogInformation("Teléfono Destino: {TelefonoDestino}", request?.TelefonoDestino);
            logger.LogInformation("Monto: ₡{Monto:N2}", request?.Monto);
            logger.LogInformation("Descripción: {Descripcion}", request?.Descripcion);
            logger.LogInformation("==========================================");

            #region PASO 1: Validar token de autorización
            var token = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                logger.LogWarning("SRV8 - Token no proporcionado");
                return Results.Unauthorized();
            }

            // Validar token (simulado)
            if (!await ValidarTokenAsync(token, httpClientFactory, config, logger))
            {
                logger.LogWarning("SRV8 - Token inválido");
                return Results.Unauthorized();
            }
            #endregion

            #region PASO 2: Validar request
            if (request == null)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Debe enviar los datos completos y válidos"
                });
            }

            // Validar campos requeridos
            if (string.IsNullOrWhiteSpace(request.TelefonoOrigen) ||
                string.IsNullOrWhiteSpace(request.NombreOrigen) ||
                string.IsNullOrWhiteSpace(request.TelefonoDestino) ||
                string.IsNullOrWhiteSpace(request.Descripcion))
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Debe enviar los datos completos y válidos"
                });
            }

            // Validar entidad origen (debe ser la del grupo)
            var grupoEntidadId = int.Parse(config["AppSettings:GrupoEntidadId"] ?? "1");
            if (request.EntidadOrigen != grupoEntidadId)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "La entidad origen debe ser la entidad del grupo"
                });
            }

            // Validar teléfono origen (debe existir en BD - simulado)
            if (!TelefonoExisteEnBD(request.TelefonoOrigen))
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El teléfono origen no está registrado en pagos móviles"
                });
            }

            // Validar teléfono destino
            if (request.TelefonoDestino.Length != 8 || !request.TelefonoDestino.All(char.IsDigit))
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El teléfono destino debe tener 8 dígitos numéricos"
                });
            }

            // Validar descripción
            if (request.Descripcion.Length > 25)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "La descripción no puede superar 25 caracteres"
                });
            }

            // Validar monto
            if (request.Monto <= 0 || request.Monto > 100000)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El monto no debe ser superior a 100.000"
                });
            }
            #endregion

            #region PASO 3: Enviar a entidad externa
            try
            {
                var externalServiceUrl = config["Services:ExternalService"] ?? "https://httpbin.org";
                logger.LogInformation("SRV8 - Enviando a entidad externa: {Url}", externalServiceUrl);

                var httpClient = httpClientFactory.CreateClient();

                var externalRequest = new
                {
                    TelefonoOrigen = request.TelefonoOrigen,
                    NombreOrigen = request.NombreOrigen,
                    TelefonoDestino = request.TelefonoDestino,
                    Monto = request.Monto,
                    Descripcion = request.Descripcion
                };

                // Simular llamada
                await Task.Delay(500);

                // Simular respuesta (80% éxito)
                var random = new Random().Next(1, 101);

                TransaccionResponse resultado;

                if (random <= 80)
                {
                    resultado = new TransaccionResponse
                    {
                        Codigo = 0,
                        Descripcion = "Transacción enviada exitosamente a entidad externa"
                    };
                    logger.LogInformation("SRV8 - Éxito al enviar a entidad externa");
                }
                else
                {
                    resultado = new TransaccionResponse
                    {
                        Codigo = -1,
                        Descripcion = "Error en entidad externa: saldo insuficiente"
                    };
                    logger.LogWarning("SRV8 - Error al enviar a entidad externa");
                }

                return resultado.Codigo == 0
                    ? Results.Ok(resultado)
                    : Results.BadRequest(resultado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SRV8 - Error al comunicarse con entidad externa");
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Error de comunicación con entidad externa"
                });
            }
            #endregion
        }

        // ==================== SRV12 HANDLER ====================
        internal static async Task<IResult> RouteTransaction(
            [FromBody] RuteoTransaccionRequest request,
            HttpContext httpContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            [FromServices] ILogger<Program> logger)
        {
            logger.LogInformation("==========================================");
            logger.LogInformation("SRV12 - Iniciando enrutamiento de transacción");
            logger.LogInformation("Teléfono Origen: {TelefonoOrigen}", request?.TelefonoOrigen);
            logger.LogInformation("Teléfono Destino: {TelefonoDestino}", request?.TelefonoDestino);
            logger.LogInformation("Monto: ₡{Monto:N2}", request?.Monto);
            logger.LogInformation("==========================================");

            #region PASO 1: Validar token
            var token = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                logger.LogWarning("SRV12 - Token no proporcionado");
                return Results.Unauthorized();
            }

            if (!await ValidarTokenAsync(token, httpClientFactory, config, logger))
            {
                logger.LogWarning("SRV12 - Token inválido");
                return Results.Unauthorized();
            }
            #endregion

            #region PASO 2: Validar request
            if (request == null)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Debe enviar los datos completos y válidos"
                });
            }

            if (string.IsNullOrWhiteSpace(request.TelefonoOrigen) ||
                string.IsNullOrWhiteSpace(request.NombreOrigen) ||
                string.IsNullOrWhiteSpace(request.TelefonoDestino) ||
                string.IsNullOrWhiteSpace(request.Descripcion))
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Debe enviar los datos completos y válidos"
                });
            }

            if (request.TelefonoOrigen.Length != 8 || !request.TelefonoOrigen.All(char.IsDigit))
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El teléfono origen debe tener 8 dígitos"
                });
            }

            if (request.TelefonoDestino.Length != 8 || !request.TelefonoDestino.All(char.IsDigit))
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El teléfono destino debe tener 8 dígitos"
                });
            }

            if (request.Descripcion.Length > 25)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "La descripción no puede superar 25 caracteres"
                });
            }

            if (request.Monto <= 0 || request.Monto > 100000)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El monto no debe ser superior a 100.000"
                });
            }
            #endregion

            #region PASO 3: Validar que teléfono origen existe
            if (!TelefonoExisteEnBD(request.TelefonoOrigen))
            {
                logger.LogWarning("SRV12 - Teléfono origen no asociado: {Telefono}", request.TelefonoOrigen);
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Cliente no asociado a pagos móviles"
                });
            }
            #endregion

            #region PASO 4: Determinar si destino es interno o externo
            var destinoEsInterno = TelefonoExisteEnBD(request.TelefonoDestino);

            if (destinoEsInterno)
            {
                // FLUJO INTERNO
                logger.LogInformation("SRV12 - Destino interno, procesando...");
                await Task.Delay(300);

                var random = new Random().Next(1, 101);

                if (random <= 90)
                {
                    return Results.Ok(new TransaccionResponse
                    {
                        Codigo = 0,
                        Descripcion = "Transacción interna procesada exitosamente"
                    });
                }
                else
                {
                    return Results.BadRequest(new TransaccionResponse
                    {
                        Codigo = -1,
                        Descripcion = "Error en core bancario: saldo insuficiente"
                    });
                }
            }
            else
            {
                // FLUJO EXTERNO
                logger.LogInformation("SRV12 - Destino externo, enviando a SRV8");

                try
                {
                    var srv8Url = config["Services:RoutingService"] ?? "https://localhost:7001";
                    var httpClient = httpClientFactory.CreateClient();

                    var externalRequest = new
                    {
                        EntidadOrigen = int.Parse(config["AppSettings:GrupoEntidadId"] ?? "1"),
                        TelefonoOrigen = request.TelefonoOrigen,
                        NombreOrigen = request.NombreOrigen,
                        TelefonoDestino = request.TelefonoDestino,
                        Monto = request.Monto,
                        Descripcion = request.Descripcion
                    };

                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    var response = await httpClient.PostAsJsonAsync($"{srv8Url}/transactions/send", externalRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<TransaccionResponse>();
                        return result != null && result.Codigo == 0
                            ? Results.Ok(result)
                            : Results.BadRequest(result);
                    }
                    else
                    {
                        return Results.BadRequest(new TransaccionResponse
                        {
                            Codigo = -1,
                            Descripcion = "Error en servicio externo"
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error llamando a SRV8");
                    return Results.BadRequest(new TransaccionResponse
                    {
                        Codigo = -1,
                        Descripcion = "Error de comunicación con servicio externo"
                    });
                }
            }
            #endregion
        }

        // ==================== SRV17 HANDLER ====================
        internal static async Task<IResult> GenerarReporteDiario(
            [FromBody] ReporteDiarioRequest request,
            HttpContext httpContext,
            [FromServices] ReporteService reporteService,
            [FromServices] ILogger<Program> logger)
        {
            logger.LogInformation("==========================================");
            logger.LogInformation("SRV17 - Generando reporte diario");
            logger.LogInformation("Fecha: {Fecha}", request?.Fecha.ToString("yyyy-MM-dd"));
            logger.LogInformation("Entidad: {EntidadId}", request?.EntidadId);
            logger.LogInformation("==========================================");

            // Extraer token del header
            var token = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var tokenParaUsar = token ?? "sistema-token-123456";

            var result = await reporteService.GenerarReporteDiarioAsync(request, tokenParaUsar);

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

            return Results.BadRequest(new ReporteDiarioResponse
            {
                Codigo = -1,
                Descripcion = "Error inesperado en el servidor"
            });
        }

        // ==================== MÉTODOS DE AYUDA ====================
        private static async Task<bool> ValidarTokenAsync(string token, IHttpClientFactory httpClientFactory, IConfiguration config, ILogger logger)
        {
            try
            {
                await Task.Delay(50);
                return !string.IsNullOrEmpty(token) && token.Length > 10;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validando token");
                return false;
            }
        }

        private static bool TelefonoExisteEnBD(string telefono)
        {
            var telefonosValidos = new[] { "88881111", "88882222", "88883333", "88884444" };
            return telefonosValidos.Contains(telefono);
        }
    }
}