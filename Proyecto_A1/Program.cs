using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using DataAccess.Models;
using DataAccess.Repositories;
using Logica_Negocio.Services;
using Proyecto_A1;
using Servicios;
using Servicios.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// =====================
// CONTEXTOS EF
// =====================
builder.Services.AddDbContext<DbContext_Tokens>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<DBContext_Bitacora>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BitacoraConnection")));

builder.Services.AddDbContext<PagosMovilesContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
// REPOSITORIOS
// =====================
builder.Services.AddScoped<IAfiliacionRepository, AfiliacionRepository>();

// =====================
// SERVICIOS
// =====================
builder.Services.AddScoped<ServicioAutenticacion>();
builder.Services.AddScoped<IBitacoraService, BitacoraService>();
builder.Services.AddScoped<IMovimientosService, MovimientosService>();
builder.Services.AddScoped<ICoreBancarioService, CoreBancarioService>();
builder.Services.AddScoped<IAfiliacionService, AfiliacionService>();

// =====================
// HTTP CLIENT
// =====================
builder.Services.AddHttpClient();

// =====================
// CONTROLLERS + SWAGGER
// =====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
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
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// =====================
// ENDPOINTS PERSONALIZADOS
// =====================
app.MapUsuariosEndpoints();
app.MapTablaPantallasEndpoints();
app.MapRolesEndpoints();
app.MapParametrosEndpoints();
app.MapEntidadesEndpoints();

// =====================
// MINIMAL API - SRV11
// =====================
app.MapGet("/accounts/transactions",
    [Microsoft.AspNetCore.Authorization.Authorize]
async (string telefono,
           string identificacion,
           IMovimientosService movimientosService,
           HttpContext httpContext) =>
    {
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
    });

app.Run();