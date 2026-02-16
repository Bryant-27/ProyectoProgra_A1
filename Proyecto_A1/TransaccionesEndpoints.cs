using Entities.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

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

            // SRV12 - Endpoint para RUTEAR transacciones (determinar si es interna o externa)
            app.MapPost("/transactions/route", RouteTransaction)
                .WithName("RouteTransaction")
                .WithOpenApi()
                .Produces<TransaccionResponse>(StatusCodes.Status200OK)
                .Produces<TransaccionResponse>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);
        }

        // ==================== SRV7 HANDLER ====================
        internal static async Task<IResult> ProcessTransaction(
            TransaccionRequest request,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<Program> logger)
        {
            logger.LogInformation("==========================================");
            logger.LogInformation("SRV7 - Solicitando procesar transacción");
            logger.LogInformation("De: {TelefonoOrigen} - {NombreOrigen}", request?.TelefonoOrigen, request?.NombreOrigen);
            logger.LogInformation("Para: {TelefonoDestino}", request?.TelefonoDestino);
            logger.LogInformation("Monto: ₡{Monto:N2}", request?.Monto);
            logger.LogInformation("==========================================");

            // Validar que la solicitud no sea nula
            if (request == null)
            {
                logger.LogWarning("SRV7 - Solicitud nula");
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
                logger.LogWarning("SRV7 - Campos requeridos incompletos");
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Debe enviar los datos completos y válidos"
                });
            }

            // Validar teléfonos (8 dígitos)
            if (request.TelefonoOrigen.Length != 8 || !request.TelefonoOrigen.All(char.IsDigit))
            {
                logger.LogWarning("SRV7 - Teléfono origen inválido: {Telefono}", request.TelefonoOrigen);
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El teléfono origen debe tener 8 dígitos numéricos"
                });
            }

            if (request.TelefonoDestino.Length != 8 || !request.TelefonoDestino.All(char.IsDigit))
            {
                logger.LogWarning("SRV7 - Teléfono destino inválido: {Telefono}", request.TelefonoDestino);
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El teléfono destino debe tener 8 dígitos numéricos"
                });
            }

            // Validar descripción (máx 25 caracteres)
            if (request.Descripcion.Length > 25)
            {
                logger.LogWarning("SRV7 - Descripción demasiado larga: {Longitud} caracteres", request.Descripcion.Length);
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "La descripción no puede superar 25 caracteres"
                });
            }

            // Validar monto
            if (request.Monto <= 0)
            {
                logger.LogWarning("SRV7 - Monto debe ser positivo: {Monto}", request.Monto);
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El monto debe ser mayor a cero"
                });
            }

            if (request.Monto > 100000)
            {
                logger.LogWarning("SRV7 - Monto excede el límite: {Monto}", request.Monto);
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El monto no debe ser superior a 100.000"
                });
            }

            // Validar entidad destino (debe ser la del grupo)
            var grupoEntidadId = config.GetValue<int>("AppSettings:GrupoEntidadId");
            if (request.EntidadDestino != grupoEntidadId)
            {
                logger.LogWarning("SRV7 - Entidad destino incorrecta. Esperada: {Esperada}, Recibida: {Recibida}",
                    grupoEntidadId, request.EntidadDestino);
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "La entidad destino no es válida"
                });
            }

            // Validar entidad origen (simulado)
            if (request.EntidadOrigen != 1)
            {
                logger.LogWarning("SRV7 - Entidad origen no registrada: {Entidad}", request.EntidadOrigen);
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "La entidad origen no está registrada o no está activa"
                });
            }

            try
            {
                // Llamar a SRV12
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

                        // Bitácora en segundo plano
                        _ = Task.Run(() => {
                            logger.LogInformation("BITACORA - Registrando transacción exitosa");
                        });

                        return Results.Ok(new TransaccionResponse
                        {
                            Codigo = 0,
                            Descripcion = "Transacción aplicada"
                        });
                    }
                    else
                    {
                        logger.LogWarning("SRV7 - SRV12 respondió con error: {Descripcion}", result?.Descripcion);

                        _ = Task.Run(() => {
                            logger.LogInformation("BITACORA - Registrando error: {Error}", result?.Descripcion);
                        });

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
            EnvioTransaccionRequest request,
            HttpContext httpContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<Program> logger)
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
            var grupoEntidadId = config.GetValue<int>("AppSettings:GrupoEntidadId");
            if (request.EntidadOrigen != grupoEntidadId)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "La entidad origen debe ser la entidad del grupo"
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
            if (request.Monto <= 0 || request.Monto > 100000)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El monto no debe ser superior a 100.000"
                });
            }

            // Validar que teléfono origen existe en BD (simulado)
            if (!TelefonoExisteEnBD(request.TelefonoOrigen))
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El teléfono origen no está registrado en pagos móviles"
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

                // SIMULACIÓN: En producción sería una llamada HTTP real
                await Task.Delay(500); // Simular latencia

                // Simular respuesta (80% éxito, 20% error)
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

                // Bitácora en segundo plano
                _ = Task.Run(() => {
                    logger.LogInformation("BITACORA - Transacción enviada a externo: {Codigo}", resultado.Codigo);
                });

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
            RuteoTransaccionRequest request,
            HttpContext httpContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<Program> logger)
        {
            logger.LogInformation("==========================================");
            logger.LogInformation("SRV12 - Iniciando enrutamiento de transacción");
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
                logger.LogWarning("SRV12 - Token no proporcionado");
                return Results.Unauthorized();
            }

            // Validar token (simulado)
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
            if (request.Monto <= 0 || request.Monto > 100000)
            {
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "El monto no debe ser superior a 100.000"
                });
            }
            #endregion

            #region PASO 3: Validar que teléfono origen está asociado a pagos móviles
            var clienteOrigen = await ObtenerClientePorTelefonoAsync(request.TelefonoOrigen);

            if (clienteOrigen == null)
            {
                logger.LogWarning("SRV12 - Teléfono origen no asociado: {Telefono}", request.TelefonoOrigen);
                return Results.BadRequest(new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Cliente no asociado a pagos móviles"
                });
            }

            logger.LogInformation("SRV12 - Cliente origen válido: {Identificacion}, Cuenta: {Cuenta}",
                clienteOrigen.Identificacion, clienteOrigen.NumeroCuenta);
            #endregion

            #region PASO 4: Verificar si teléfono destino está en pagos móviles
            var clienteDestino = await ObtenerClientePorTelefonoAsync(request.TelefonoDestino);

            // Bitácora en segundo plano (inicio)
            _ = Task.Run(() => {
                logger.LogInformation("BITACORA - SRV12 procesando transacción {TelefonoOrigen} -> {TelefonoDestino}",
                    request.TelefonoOrigen, request.TelefonoDestino);
            });

            if (clienteDestino != null)
            {
                // ===== FLUJO INTERNO: Destino está en mi entidad =====
                logger.LogInformation("SRV12 - Destino encontrado en pagos móviles. Procesando internamente...");

                // Llamar a SRV14 (Core Bancario - Crédito)
                var resultadoCore = await LlamarSRV14Async(new CreditoRequest
                {
                    Identificacion = clienteDestino.Identificacion,
                    Cuenta = clienteDestino.NumeroCuenta,
                    Monto = request.Monto,
                    Tipo = "CREDITO"
                }, httpClientFactory, config, logger);

                if (resultadoCore.Codigo == 0)
                {
                    logger.LogInformation("SRV12 - Crédito aplicado exitosamente en core bancario");

                    return Results.Ok(new TransaccionResponse
                    {
                        Codigo = 0,
                        Descripcion = "Transacción interna procesada exitosamente"
                    });
                }
                else
                {
                    logger.LogWarning("SRV12 - Error en core bancario: {Descripcion}", resultadoCore.Descripcion);

                    return Results.BadRequest(new TransaccionResponse
                    {
                        Codigo = -1,
                        Descripcion = resultadoCore.Descripcion
                    });
                }
            }
            else
            {
                // ===== FLUJO EXTERNO: Destino está en otra entidad =====
                logger.LogInformation("SRV12 - Destino NO encontrado en pagos móviles. Enviando a entidad externa...");

                // Llamar a SRV8 para enviar a entidad externa
                var resultadoExterno = await LlamarSRV8Async(new EnvioTransaccionRequest
                {
                    EntidadOrigen = config.GetValue<int>("AppSettings:GrupoEntidadId"),
                    TelefonoOrigen = request.TelefonoOrigen,
                    NombreOrigen = request.NombreOrigen,
                    TelefonoDestino = request.TelefonoDestino,
                    Monto = request.Monto,
                    Descripcion = request.Descripcion
                }, token, httpClientFactory, config, logger);

                if (resultadoExterno.Codigo == 0)
                {
                    logger.LogInformation("SRV12 - Transacción externa procesada exitosamente");

                    return Results.Ok(new TransaccionResponse
                    {
                        Codigo = 0,
                        Descripcion = resultadoExterno.Descripcion
                    });
                }
                else
                {
                    logger.LogWarning("SRV12 - Error en entidad externa: {Descripcion}", resultadoExterno.Descripcion);

                    return Results.BadRequest(new TransaccionResponse
                    {
                        Codigo = -1,
                        Descripcion = resultadoExterno.Descripcion
                    });
                }
            }
            #endregion
        }

        // ==================== MÉTODOS DE AYUDA ====================

        private static async Task<bool> ValidarTokenAsync(string token, IHttpClientFactory httpClientFactory, IConfiguration config, ILogger logger)
        {
            try
            {
                // SIMULACIÓN: En producción llamaría a SRV5: /auth/validate
                await Task.Delay(50);

                // Token válido si tiene más de 10 caracteres (simulación)
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
            // SIMULACIÓN: En producción consultaría a la base de datos (tabla Afiliacion)
            var telefonosValidos = new[] { "88881111", "88882222", "88883333", "88884444" };
            return telefonosValidos.Contains(telefono);
        }

        private static async Task<ClienteInfo> ObtenerClientePorTelefonoAsync(string telefono)
        {
            // SIMULACIÓN: En producción consultaría a la base de datos (tabla Afiliacion, Usuarios, Cuentas)
            await Task.Delay(50);

            // Datos simulados de clientes
            var clientes = new Dictionary<string, ClienteInfo>
            {
                ["88881111"] = new ClienteInfo
                {
                    Identificacion = "101110111",
                    NumeroCuenta = "CR012345678901234567",
                    Nombre = "Juan Perez"
                },
                ["88882222"] = new ClienteInfo
                {
                    Identificacion = "202220222",
                    NumeroCuenta = "CR987654321098765432",
                    Nombre = "Maria Lopez"
                },
                ["88883333"] = new ClienteInfo
                {
                    Identificacion = "303330333",
                    NumeroCuenta = "CR456789012345678901",
                    Nombre = "Carlos Ruiz"
                }
            };

            return clientes.ContainsKey(telefono) ? clientes[telefono] : null;
        }

        private static async Task<TransaccionResponse> LlamarSRV14Async(CreditoRequest request, IHttpClientFactory httpClientFactory, IConfiguration config, ILogger logger)
        {
            try
            {
                var coreServiceUrl = config["Services:CoreService"] ?? "https://localhost:7002";
                logger.LogInformation("SRV12 - Llamando a SRV14 (Core Bancario): {Url}/api/cuenta/credito", coreServiceUrl);

                var httpClient = httpClientFactory.CreateClient();

                // SIMULACIÓN: En producción sería una llamada HTTP real
                await Task.Delay(300);

                // Simular respuesta (90% éxito)
                var random = new Random().Next(1, 101);

                if (random <= 90)
                {
                    return new TransaccionResponse
                    {
                        Codigo = 0,
                        Descripcion = "Crédito aplicado correctamente"
                    };
                }
                else
                {
                    return new TransaccionResponse
                    {
                        Codigo = -1,
                        Descripcion = "Error en core bancario: saldo insuficiente en cuenta destino"
                    };
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error llamando a SRV14");
                return new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Error de comunicación con core bancario"
                };
            }
        }

        private static async Task<TransaccionResponse> LlamarSRV8Async(EnvioTransaccionRequest request, string token, IHttpClientFactory httpClientFactory, IConfiguration config, ILogger logger)
        {
            try
            {
                var srv8Url = config["Services:RoutingService"] ?? "https://localhost:7001";
                logger.LogInformation("SRV12 - Llamando a SRV8: {Url}/transactions/send", srv8Url);

                var httpClient = httpClientFactory.CreateClient();

                // SIMULACIÓN: En producción sería una llamada HTTP real con token
                await Task.Delay(400);

                // Simular respuesta (80% éxito)
                var random = new Random().Next(1, 101);

                if (random <= 80)
                {
                    return new TransaccionResponse
                    {
                        Codigo = 0,
                        Descripcion = "Transacción enviada a entidad externa"
                    };
                }
                else
                {
                    return new TransaccionResponse
                    {
                        Codigo = -1,
                        Descripcion = "Error en entidad externa"
                    };
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error llamando a SRV8");
                return new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Error de comunicación con servicio externo"
                };
            }
        }
    }

    // ==================== CLASES AUXILIARES ====================

    public class LoginRequest
    {
        public string? Usuario { get; set; }
        public string? Contraseña { get; set; }
    }

    public class ClienteInfo
    {
        public string Identificacion { get; set; }
        public string NumeroCuenta { get; set; }
        public string Nombre { get; set; }
    }

    public class CreditoRequest
    {
        public string Identificacion { get; set; }
        public string Cuenta { get; set; }
        public decimal Monto { get; set; }
        public string Tipo { get; set; }
    }
}