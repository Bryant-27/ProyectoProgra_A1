using DataAccess.Models;
using Logica_Negocio.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Proyecto_A1;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // Agregar soporte para controllers
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API Pagos Móviles",
        Version = "v1",
        Description = "API para sistema de pagos móviles - Historias SRV7, SRV8, SRV12, SRV17"
    });

    // Configurar autenticación JWT en Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configurar DbContext
builder.Services.AddDbContext<PagosMovilesContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar servicios
builder.Services.AddScoped<TransaccionService>();
builder.Services.AddScoped<AuthService>();

// Configurar autenticación JWT
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    jwtKey = "SuperSecretKeyForPagosMovilesAPIMinimo32Characters1234567890";
    Console.WriteLine("Advertencia: Usando clave JWT por defecto. Configura 'Jwt:Key' en appsettings.json para producción.");
}

var key = Encoding.ASCII.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false, // Cambiar a true en producción
        ValidateAudience = false, // Cambiar a true en producción
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Para depuración: log de errores de token
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token validated successfully");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    // Política por defecto requiere autenticación
    options.FallbackPolicy = options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Pagos Móviles v1");
        c.RoutePrefix = string.Empty; // Para acceder en raíz: http://localhost:xxxx/
    });
}

app.UseHttpsRedirection();

// Habilitar CORS
app.UseCors("AllowAll");

// Middleware de autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Mapear endpoints existentes
app.MapUsuariosEndpoints();

// Mapear nuevos endpoints de transacciones
app.MapTransaccionesEndpoints();

// Endpoint de prueba
app.MapGet("/", () => "API Pagos Móviles - SRV7, SRV8, SRV12, SRV17 funcionando")
    .AllowAnonymous()
    .WithName("API Home")
    .WithOpenApi();

// Endpoint de salud
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.Now,
        services = new[] { "SRV7", "SRV8", "SRV12", "SRV17" }
    });
})
.AllowAnonymous()
.WithName("Health Check")
.WithOpenApi();

// Endpoint de login (HU SRV5)
app.MapPost("/api/login", async (
    HttpContext httpContext,
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
            expires_in = 300, // 5 minutos en segundos
            refresh_token = Guid.NewGuid().ToString(), // Simplificado
            usuarioID = result.UsuarioId
        });
    }

    return Results.Unauthorized();
})
.AllowAnonymous()
.WithName("Login")
.WithOpenApi();

app.Run();

// DTO para login
public class LoginRequest
{
    public string Usuario { get; set; } = null!;
    public string Contrasena { get; set; } = null!;
}
