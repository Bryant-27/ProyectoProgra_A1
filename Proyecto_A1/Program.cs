using DataAccess.Models;
using DataAccess.Repositories;
using Entities.DTOs;
using Logica_Negocio.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pagos Móviles API - SRV7, SRV8, SRV12, SRV18",
        Version = "v1",
        Description = "API para recibir, enviar, rutear transacciones y gestionar bitácoras"
    });

    // Configurar para enviar token en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Registrar HttpClient
builder.Services.AddHttpClient();

// Registrar DbContexts
builder.Services.AddDbContext<CoreBancarioContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CoreBancarioConnection")));

builder.Services.AddDbContext<PagosMovilesContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PagosMovilesConnection")));

// Registrar repositorios
builder.Services.AddScoped<BitacoraRepository>();

// Registrar servicios
builder.Services.AddScoped<BitacoraService>();

var app = builder.Build();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pagos Móviles API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

// ==================== SRV7 - RECIBIR TRANSACCIONES (PÚBLICO) ====================
app.MapPost("/transactions/process", async (
    HttpContext httpContext,
    [FromBody] TransaccionRequest request,
    IHttpClientFactory httpClientFactory,
    IConfiguration config) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

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
    var grupoEntidadId = config.GetValue<int>("AppSettings:GrupoEntidadId");
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

                // Registrar en bitácora SRV18
                try
                {
                    var bitacoraService = app.Services.CreateScope().ServiceProvider.GetRequiredService<BitacoraService>();
                    var token = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

                    _ = Task.Run(async () => {
                        await bitacoraService.RegistrarAsync(new BitacoraRequest
                        {
                            Usuario = "SYSTEM",
                            Accion = "SRV7_PROCESS",
                            Descripcion = $"Transacción procesada: {request.TelefonoOrigen} -> {request.TelefonoDestino} por ₡{request.Monto}"
                        }, token ?? "sistema-token-123456");
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error registrando en bitácora");
                }

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
})
.WithName("ProcessTransaction")
.WithOpenApi()
.Produces<TransaccionResponse>(StatusCodes.Status200OK)
.Produces<TransaccionResponse>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError)
.AllowAnonymous();


// ==================== SRV8 - ENVIAR TRANSACCIONES A ENTIDADES EXTERNAS ====================
app.MapPost("/transactions/send", async (
    [FromBody] EnvioTransaccionRequest request,
    HttpContext httpContext,
    IHttpClientFactory httpClientFactory,
    IConfiguration config) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

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

    // Validar token con SRV5 (simulado)
    var tokenValido = await ValidarTokenAsync(token, httpClientFactory, config, logger);
    if (!tokenValido)
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

        // Preparar request para el servicio externo
        var externalRequest = new
        {
            TelefonoOrigen = request.TelefonoOrigen,
            NombreOrigen = request.NombreOrigen,
            TelefonoDestino = request.TelefonoDestino,
            Monto = request.Monto,
            Descripcion = request.Descripcion
        };

        // Llamada real a httpbin.org para pruebas
        var response = await httpClient.PostAsJsonAsync($"{externalServiceUrl}/post", externalRequest);

        TransaccionResponse resultado;

        if (response.IsSuccessStatusCode)
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
                Descripcion = $"Error en entidad externa: {response.StatusCode}"
            };
            logger.LogWarning("SRV8 - Error al enviar a entidad externa");
        }

        // Registrar en bitácora SRV18
        try
        {
            var bitacoraService = app.Services.CreateScope().ServiceProvider.GetRequiredService<BitacoraService>();

            _ = Task.Run(async () => {
                await bitacoraService.RegistrarAsync(new BitacoraRequest
                {
                    Usuario = "SYSTEM",
                    Accion = "SRV8_SEND",
                    Descripcion = $"Transacción enviada a externo: {request.TelefonoOrigen} -> {request.TelefonoDestino} - Resultado: {resultado.Codigo}"
                }, token);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registrando en bitácora");
        }

        return resultado.Codigo == 0
            ? Results.Ok(resultado)
            : Results.BadRequest(resultado);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "SRV8 - Error al comunicarse con entidad externa");

        // Registrar error en bitácora
        try
        {
            var bitacoraService = app.Services.CreateScope().ServiceProvider.GetRequiredService<BitacoraService>();

            _ = Task.Run(async () => {
                await bitacoraService.RegistrarAsync(new BitacoraRequest
                {
                    Usuario = "SYSTEM",
                    Accion = "SRV8_ERROR",
                    Descripcion = $"Error comunicación externa: {ex.Message}"
                }, token);
            });
        }
        catch { }

        return Results.BadRequest(new TransaccionResponse
        {
            Codigo = -1,
            Descripcion = "Error de comunicación con entidad externa"
        });
    }
    #endregion
})
.WithName("SendTransaction")
.WithOpenApi()
.Produces<TransaccionResponse>(StatusCodes.Status200OK)
.Produces<TransaccionResponse>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status500InternalServerError);


// ==================== SRV12 - RUTEAR TRANSACCIONES ====================
app.MapPost("/transactions/route", async (
    [FromBody] RuteoTransaccionRequest request,
    HttpContext httpContext,
    IHttpClientFactory httpClientFactory,
    IConfiguration config) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

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
    // Simulación - en producción consultaría a la base de datos
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
        // FLUJO INTERNO - Llamar a SRV14 (simulado)
        logger.LogInformation("SRV12 - Destino interno, procesando con SRV14");

        // Simular llamada a core bancario
        await Task.Delay(300);

        // Simular 90% éxito
        var random = new Random().Next(1, 101);

        if (random <= 90)
        {
            // Registrar bitácora
            await RegistrarBitacora(app, "SRV12_INTERNAL",
                $"Transacción interna exitosa: {request.TelefonoOrigen} -> {request.TelefonoDestino}", token);

            return Results.Ok(new TransaccionResponse
            {
                Codigo = 0,
                Descripcion = "Transacción interna procesada exitosamente"
            });
        }
        else
        {
            await RegistrarBitacora(app, "SRV12_INTERNAL_ERROR",
                $"Error en core bancario: saldo insuficiente", token);

            return Results.BadRequest(new TransaccionResponse
            {
                Codigo = -1,
                Descripcion = "Error en core bancario: saldo insuficiente"
            });
        }
    }
    else
    {
        // FLUJO EXTERNO - Llamar a SRV8
        logger.LogInformation("SRV12 - Destino externo, enviando a SRV8");

        try
        {
            var srv8Url = config["Services:RoutingService"] ?? "https://localhost:7001";
            var httpClient = httpClientFactory.CreateClient();

            var externalRequest = new
            {
                EntidadOrigen = config.GetValue<int>("AppSettings:GrupoEntidadId"),
                TelefonoOrigen = request.TelefonoOrigen,
                NombreOrigen = request.NombreOrigen,
                TelefonoDestino = request.TelefonoDestino,
                Monto = request.Monto,
                Descripcion = request.Descripcion
            };

            // Llamar a SRV8 con el mismo token
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            var response = await httpClient.PostAsJsonAsync($"{srv8Url}/transactions/send", externalRequest);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TransaccionResponse>();

                await RegistrarBitacora(app, "SRV12_EXTERNAL",
                    $"Transacción externa enviada a SRV8", token);

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
})
.WithName("RouteTransaction")
.WithOpenApi()
.Produces<TransaccionResponse>(StatusCodes.Status200OK)
.Produces<TransaccionResponse>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status500InternalServerError);


// ==================== SRV18 - BITÁCORA ====================
app.MapPost("/bitacora", async (
    [FromBody] BitacoraRequest request,
    HttpContext httpContext,
    [FromServices] BitacoraService bitacoraService) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("SRV18 - Registrando bitácora");

    var token = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

    var result = await bitacoraService.RegistrarAsync(request, token);

    return result.Codigo == 0
        ? Results.Created($"/bitacora/{result.BitacoraId}", result)
        : result.Descripcion.Contains("Token")
            ? Results.Unauthorized()
            : Results.BadRequest(result);
})
.WithName("RegistrarBitacora")
.WithOpenApi()
.Produces<BitacoraResponse>(StatusCodes.Status201Created)
.Produces<BitacoraResponse>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status500InternalServerError);

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    status = "API funcionando",
    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
    endpoints = new[] {
        "POST /transactions/process (SRV7 - público)",
        "POST /transactions/send (SRV8 - requiere token)",
        "POST /transactions/route (SRV12 - requiere token)",
        "POST /bitacora (SRV18 - requiere token)",
        "GET /health"
    }
}))
.WithName("HealthCheck")
.WithOpenApi()
.AllowAnonymous();

app.Run();

// ==================== MÉTODOS DE AYUDA ====================

async Task<bool> ValidarTokenAsync(string token, IHttpClientFactory httpClientFactory, IConfiguration config, ILogger logger)
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

bool TelefonoExisteEnBD(string telefono)
{
    var telefonosValidos = new[] { "88881111", "88882222", "88883333", "88884444" };
    return telefonosValidos.Contains(telefono);
}

async Task RegistrarBitacora(WebApplication app, string accion, string descripcion, string token)
{
    try
    {
        var bitacoraService = app.Services.CreateScope().ServiceProvider.GetRequiredService<BitacoraService>();
        await bitacoraService.RegistrarAsync(new BitacoraRequest
        {
            Usuario = "SYSTEM",
            Accion = accion,
            Descripcion = descripcion
        }, token ?? "sistema-token-123456");
    }
    catch { }
}

// ==================== DTOs ====================
public class EnvioTransaccionRequest
{
    public int EntidadOrigen { get; set; }
    public string TelefonoOrigen { get; set; }
    public string NombreOrigen { get; set; }
    public string TelefonoDestino { get; set; }
    public decimal Monto { get; set; }
    public string Descripcion { get; set; }
}

public class RuteoTransaccionRequest
{
    public string TelefonoOrigen { get; set; }
    public string NombreOrigen { get; set; }
    public string TelefonoDestino { get; set; }
    public decimal Monto { get; set; }
    public string Descripcion { get; set; }
}

public partial class Program { }