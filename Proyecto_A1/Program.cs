using Microsoft.AspNetCore.Authentication.Negotiate;
using Proyecto_A1;
using DataAccess.Models;
using Servicios;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

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


