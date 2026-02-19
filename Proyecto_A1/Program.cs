using Abstract.Interfaces;
using DataAccess.Models;
using DataAccess.Repositories;
using Entities.DTOs;
using Logica_Negocio.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Proyecto_A1;
using Servicios;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);

// =====================
// CONTEXTOS EF
// =====================
builder.Services.AddDbContext<DbContext_Tokens>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<DBContext_Bitacora>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BitacoraConnection")));

builder.Services.AddDbContext<PagosMovilesContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PagosMovilesConnection")));

builder.Services.AddDbContext<CoreBancarioContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CoreBancarioConnection")));

// =====================
// JWT AUTHENTICATION (ÚNICO)
// =====================
var secretKey = builder.Configuration["Settings:SecretKey"]
                ?? throw new InvalidOperationException("Falta Settings:SecretKey en configuración.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// =====================
// HTTP CLIENT
// =====================
builder.Services.AddHttpClient();

// =====================
// REPOSITORIOS
// =====================
builder.Services.AddScoped<IAfiliacionRepository, AfiliacionRepository>();
builder.Services.AddScoped<TransaccionRepository>();
//builder.Services.AddScoped<BitacoraRepository>();

// =====================
// SERVICIOS
// =====================
builder.Services.AddScoped<ServicioAutenticacion>();
builder.Services.AddScoped<IBitacoraService, BitacoraService>();
builder.Services.AddScoped<IMovimientosService, MovimientosService>();
builder.Services.AddScoped<ICoreBancarioService, CoreBancarioService>();
builder.Services.AddScoped<IAfiliacionService, AfiliacionService>();
builder.Services.AddScoped<ReporteService>(); ;

// =====================
// CONTROLLERS + SWAGGER
// =====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pagos Móviles API - SRV7, SRV8, SRV11, SRV12, SRV17",
        Version = "v1",
        Description = "API para recibir, enviar, rutear transacciones, consultar movimientos y generar reportes"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// =====================
// PIPELINE
// =====================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pagos Móviles API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// =====================
// ENDPOINTS PERSONALIZADOS (Proyecto_A1)
// =====================
app.MapUsuariosEndpoints();
app.MapTablaPantallasEndpoints();
app.MapRolesEndpoints();
app.MapParametrosEndpoints();
app.MapEntidadesEndpoints();

// =====================
// MINIMAL APIs - SERVICIOS DE TRANSACCIONES
// =====================

//  SRV7 - RECIBIR TRANSACCIONES (PÚBLICO) 
app.MapPost("/transactions/process", async (
    HttpContext httpContext,
    [FromBody] TransaccionRequest request,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    [FromServices] AfiliacionRepository afiliacionRepo,
    ILogger<Program> logger) =>
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

    // Verificar que la entidad origen existe en la BD
    var entidadOrigen = await afiliacionRepo.ObtenerEntidadPorIdAsync(request.EntidadOrigen);
    if (entidadOrigen == null)
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
        var routingServiceUrl = config["Services:RoutingService"] ?? "http://localhost:5104";
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
})
.WithName("ProcessTransaction")
.WithOpenApi()
.Produces<TransaccionResponse>(StatusCodes.Status200OK)
.Produces<TransaccionResponse>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError)
.AllowAnonymous();


//  SRV8 - ENVIAR TRANSACCIONES A ENTIDADES EXTERNAS 
app.MapPost("/transactions/send", async (
    [FromBody] EnvioTransaccionRequest request,
    HttpContext httpContext,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    [FromServices] AfiliacionRepository afiliacionRepo,
    ILogger<Program> logger) =>
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

    // Validar token (simulado - hasta implementar SRV5)
    if (token.Length <= 10)
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

    // Validar que el teléfono origen existe en la BD
    var afiliacion = await afiliacionRepo.ObtenerPorTelefonoAsync(request.TelefonoOrigen);
    if (afiliacion == null)
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

        // Simular llamada (en producción sería una llamada HTTP real)
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
})
.WithName("SendTransaction")
.WithOpenApi()
.Produces<TransaccionResponse>(StatusCodes.Status200OK)
.Produces<TransaccionResponse>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status500InternalServerError);


//  SRV11 - CONSULTAR MOVIMIENTOS 
app.MapGet("/accounts/transactions",
    [Microsoft.AspNetCore.Authorization.Authorize]
async (string telefono,
       string identificacion,
       IMovimientosService movimientosService,
       HttpContext httpContext,
       ILogger<Program> logger) =>
    {
        logger.LogInformation("==========================================");
        logger.LogInformation("SRV11 - Consultando movimientos");
        logger.LogInformation("Teléfono: {Telefono}", telefono);
        logger.LogInformation("Identificación: {Identificacion}", identificacion);
        logger.LogInformation("==========================================");

        if (string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(identificacion))
            return Results.BadRequest(new { codigo = -1, descripcion = "Datos incompletos" });

        var usuario = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                   ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                   ?? "Sistema";

        var resultado = await movimientosService.ObtenerUltimosMovimientosAsync(telefono, identificacion, usuario);

        return resultado.Codigo == -1
            ? Results.BadRequest(new { resultado.Codigo, resultado.Descripcion })
            : Results.Ok(new
            {
                codigo = 0,
                descripcion = "Consulta exitosa",
                movimientos = resultado.Movimientos
            });
    })
.WithName("GetAccountTransactions")
.WithOpenApi()
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized);


// SRV12 - RUTEAR TRANSACCIONES 
app.MapPost("/transactions/route", async (
    [FromBody] RuteoTransaccionRequest request,
    HttpContext httpContext,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    [FromServices] AfiliacionRepository afiliacionRepo,
    ILogger<Program> logger) =>
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

    if (token.Length <= 10)
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

    #region PASO 3: Validar que teléfono origen existe (CONSULTA REAL BD)
    var afiliacionOrigen = await afiliacionRepo.ObtenerPorTelefonoAsync(request.TelefonoOrigen);
    if (afiliacionOrigen == null)
    {
        logger.LogWarning("SRV12 - Teléfono origen no asociado: {Telefono}", request.TelefonoOrigen);
        return Results.BadRequest(new TransaccionResponse
        {
            Codigo = -1,
            Descripcion = "Cliente no asociado a pagos móviles"
        });
    }
    #endregion

    #region PASO 4: Determinar si destino es interno o externo (CONSULTA REAL BD)
    var afiliacionDestino = await afiliacionRepo.ObtenerPorTelefonoAsync(request.TelefonoDestino);
    var destinoEsInterno = afiliacionDestino != null;

    if (destinoEsInterno)
    {
        // FLUJO INTERNO
        logger.LogInformation("SRV12 - Destino interno, procesando...");
        logger.LogInformation("Cliente destino: {Identificacion}, Cuenta: {Cuenta}",
            afiliacionDestino.IdentificacionUsuario, afiliacionDestino.NumeroCuenta);

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
            var srv8Url = config["Services:RoutingService"] ?? "http://localhost:5104";
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
})
.WithName("RouteTransaction")
.WithOpenApi()
.Produces<TransaccionResponse>(StatusCodes.Status200OK)
.Produces<TransaccionResponse>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status500InternalServerError);


//  SRV17 - REPORTE DIARIO 
app.MapPost("/reportes/diario", async (
    [FromBody] ReporteDiarioRequest request,
    HttpContext httpContext,
    [FromServices] ReporteService reporteService,
    ILogger<Program> logger) =>
{
    logger.LogInformation("==========================================");
    logger.LogInformation("SRV17 - Generando reporte diario");
    logger.LogInformation("Fecha: {Fecha}", request?.Fecha.ToString("yyyy-MM-dd"));
    logger.LogInformation("Entidad: {EntidadId}", request?.EntidadId);
    logger.LogInformation("==========================================");

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
})
.WithName("GenerarReporteDiario")
.WithOpenApi()
.Produces<ReporteDiarioResponse>(StatusCodes.Status200OK)
.Produces<ReporteDiarioResponse>(StatusCodes.Status400BadRequest)
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
        "GET /accounts/transactions (SRV11 - requiere token)",
        "POST /transactions/route (SRV12 - requiere token)",
        "POST /reportes/diario (SRV17 - requiere token)",
        "GET /health"
    }
}))
.WithName("HealthCheck")
.WithOpenApi()
.AllowAnonymous();

app.Run();

// DTOs locales (mover a Entities.DTOs en una refactorización posterior)
public class EnvioTransaccionRequest
{
    public int EntidadOrigen { get; set; }
    public string TelefonoOrigen { get; set; } = string.Empty;
    public string NombreOrigen { get; set; } = string.Empty;
    public string TelefonoDestino { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public class RuteoTransaccionRequest
{
    public string TelefonoOrigen { get; set; } = string.Empty;
    public string NombreOrigen { get; set; } = string.Empty;
    public string TelefonoDestino { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public partial class Program { }