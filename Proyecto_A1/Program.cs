using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Proyecto_A1;
using DataAccess.Models;
using Servicios;
using Microsoft.EntityFrameworkCore;
using DataAccess.Repositories;
using Logica_Negocio.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =====================
// CONTEXTO TOKENS
// =====================
builder.Services.AddDbContext<DbContext_Tokens>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// =====================
// CONTEXTO BITÁCORA
// =====================
builder.Services.AddDbContext<DBContext_Bitacora>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("BitacoraConnection")));

// =====================
// CONTEXTO PAGOS MÓVILES
// =====================
builder.Services.AddDbContext<PagosMovilesContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// =====================
// HTTP CLIENT (para SRV11 - llamar al Core)
// =====================
builder.Services.AddHttpClient();

// =====================
// REPOSITORIOS (SRV11)
// =====================
builder.Services.AddScoped<IAfiliacionRepository, AfiliacionRepository>();

// =====================
// SERVICIOS (SRV11)
// =====================
builder.Services.AddScoped<ServicioAutenticacion>();
builder.Services.AddScoped<IBitacoraService, BitacoraService>();
builder.Services.AddScoped<IMovimientosService, MovimientosService>();

// =====================
// JWT AUTHENTICATION (REEMPLAZA NEGOTIATE)
// =====================
var secretKey = builder.Configuration["Settings:SecretKey"]
    ?? "ClaveSuperSecretaDeMasDe32Caracteres123456";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// =====================
// CONTROLLERS + SWAGGER
// =====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger con JWT
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT: Bearer {tu_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// =====================
// MAP CONTROLLERS Y ENDPOINTS
// =====================
app.MapControllers();
app.MapUsuariosEndpoints();

// =====================
// SRV11 - ENDPOINT MINIMAL API
// =====================
app.MapGet("/accounts/transactions", [Microsoft.AspNetCore.Authorization.Authorize] async (
    string telefono,
    string identificacion,
    IMovimientosService movimientosService,
    HttpContext httpContext) =>
{
    // Validaciones
    if (string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(identificacion))
    {
        return Results.BadRequest(new
        {
            codigo = -1,
            descripcion = "Debe enviar los datos completos y válidos"
        });
    }

    // Obtener usuario del token
    var usuario = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
        ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? "Sistema";

    // Llamar servicio SRV11
    var resultado = await movimientosService.ObtenerUltimosMovimientosAsync(
        telefono, identificacion, usuario);

    if (resultado.Codigo == -1)
    {
        return Results.BadRequest(new
        {
            codigo = resultado.Codigo,
            descripcion = resultado.Descripcion
        });
    }

    return Results.Ok(new
    {
        codigo = 0,
        descripcion = "Consulta exitosa",
        movimientos = resultado.Movimientos
    });
});

app.Run();