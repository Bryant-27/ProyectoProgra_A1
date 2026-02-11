using Microsoft.AspNetCore.Authentication.Negotiate;
using Proyecto_A1;
using DataAccess.Models;
using Servicios;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// =====================
// CONTEXTO TOKENS
// =====================

builder.Services.AddDbContext<DbContext_Tokens>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    //var key = Encoding.ASCII.GetBytes(
    //    builder.Configuration["Settings:SecretKey"]);

    var key = builder.Configuration["Settings:SecretKey"];

    if (string.IsNullOrEmpty(key))
    {
        throw new Exception("La clave secreta para JWT no puede estar vacía. Verifique la configuración.");
    }
        
    var secretkey = Encoding.ASCII.GetBytes(key);

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretkey),

    };
});

// =====================
// CONTEXTO BITÁCORA
// =====================
builder.Services.AddDbContext<DBContext_Bitacora>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("BitacoraConnection")));

// =====================
// OTRO CONTEXTO
// =====================
builder.Services.AddDbContext<PagosMovilesContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// =====================
// SERVICIOS
// =====================
builder.Services.AddScoped<ServicioAutenticacion>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingrese el token JWT generado por el endpoint de autenticación. Ejemplo Bearer {el token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
        new string[] { }
    }

});
});


//builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
//    .AddNegotiate();

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapUsuariosEndpoints();

app.Run();


