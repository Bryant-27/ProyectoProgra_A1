using Entities.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
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
        Title = "Pagos Móviles API - SRV7 y SRV8",
        Version = "v1",
        Description = "API para recibir y enviar transacciones de pagos móviles"
    });
});

// Registrar HttpClient
builder.Services.AddHttpClient();

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

    // TODO: Aquí llamarías a SRV12 para enrutar
    // Por ahora simulamos éxito

    // Bitácora simulada
    _ = Task.Run(() => {
        logger.LogInformation("BITACORA - Transacción recibida y procesada");
    });

    return Results.Ok(new TransaccionResponse
    {
        Codigo = 0,
        Descripcion = "Transacción aplicada"
    });
})
.WithName("ProcessTransaction")
.WithOpenApi()
.Produces<TransaccionResponse>(StatusCodes.Status200OK)
.Produces<TransaccionResponse>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError)
.AllowAnonymous(); // Público - sin token


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
        var externalServiceUrl = config["Services:ExternalService"] ?? "https://api.bancoexterno.com";
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

        // Simular llamada a servicio externo (en producción sería una llamada HTTP real)
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

        // Bitácora
        _ = Task.Run(() => {
            logger.LogInformation("BITACORA - Transacción enviada a externo: {Codigo}", resultado.Codigo);
        });

        // Retornar la misma respuesta del servicio externo
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
})
.WithName("SendTransaction")
.WithOpenApi()
.Produces<TransaccionResponse>(StatusCodes.Status200OK)
.Produces<TransaccionResponse>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status500InternalServerError);

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    status = "API SRV7 y SRV8 funcionando",
    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
    endpoints = new[] {
        "POST /transactions/process (público)",
        "POST /transactions/send (requiere token)"
    }
}))
.WithName("HealthCheck")
.WithOpenApi()
.AllowAnonymous();

app.Run();

// ==================== MÉTODOS DE AYUDA ====================

// Método para validar token con SRV5
async Task<bool> ValidarTokenAsync(string token, IHttpClientFactory httpClientFactory, IConfiguration config, ILogger logger)
{
    try
    {
        // En producción, esto llamaría a SRV5: /auth/validate
        // Simulación: token válido si tiene más de 10 caracteres
        await Task.Delay(50); // Simular latencia

        // Simulación simple - en producción esto llamaría a un servicio real
        return !string.IsNullOrEmpty(token) && token.Length > 10;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error validando token");
        return false;
    }
}

// Método para simular validación de teléfono en BD
bool TelefonoExisteEnBD(string telefono)
{
    // Simulación - en producción consultaría a la base de datos
    // Aquí asumimos que algunos teléfonos existen
    var telefonosValidos = new[] { "88881111", "88882222", "88883333", "88884444" };
    return telefonosValidos.Contains(telefono);
}


// ==================== DTOs ADICIONALES PARA SRV8 ====================
public class EnvioTransaccionRequest
{
    public int EntidadOrigen { get; set; }
    public string TelefonoOrigen { get; set; }
    public string NombreOrigen { get; set; }
    public string TelefonoDestino { get; set; }
    public decimal Monto { get; set; }
    public string Descripcion { get; set; }
}

public partial class Program { }