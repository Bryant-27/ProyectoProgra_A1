using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Logica_Negocio.Interfaces;
using Logica_Negocio.Models;

namespace Logica_Negocio.Services
{
    public class ServicioAutenticacion : IAutenticacionService
    {
        private readonly DbContext_Tokens _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServicioAutenticacion> _logger;
        private readonly IBitacoraService _bitacoraService;

        // Constructor público con todas las dependencias
        public ServicioAutenticacion(
            DbContext_Tokens context,
            IConfiguration configuration,
            ILogger<ServicioAutenticacion> logger,
            IBitacoraService bitacoraService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _bitacoraService = bitacoraService;
        }

        public async Task<SesionUsuario?> ValidarYAutenticar(string idUsuario, string nombre, string password)
        {
            try
            {
                _logger.LogInformation("Validando autenticación para usuario: {Nombre}", nombre);

                // Validar credenciales contra la base de datos
                int idUsuarioInt = int.Parse(idUsuario);

                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.IdUsuario == idUsuarioInt &&
                                              u.Usuario == nombre &&
                                              u.Contraseña == password);

                if (usuario == null)
                {
                    _logger.LogWarning("Credenciales inválidas para usuario: {Nombre}", nombre);

                    await _bitacoraService.RegistrarAsync(
                        usuario: nombre,
                        accion: "LOGIN",
                        resultado: "FALLIDO",
                        descripcion: "Intento de login fallido - credenciales inválidas",
                        servicio: "ServicioAutenticacion.ValidarYAutenticar"
                    );

                    return null;
                }

                // Generar token JWT
                var token = GenerarToken(usuario.IdUsuario.ToString(), usuario.Usuario);
                var refreshToken = GenerarRefreshToken();
                var fechaExpiracion = DateTime.UtcNow.AddHours(8);
                var fechaExpiracionRefresh = DateTime.UtcNow.AddDays(7);

                // Guardar sesión en base de datos
                var sesion = new InicioSesion
                {
                    IdUsuario = usuario.IdUsuario,
                    JwtToken = token,
                    RefreshToken = refreshToken,
                    FechaInico = DateTime.UtcNow,
                    FechaExpiracionToken = fechaExpiracion,
                    FechaExpiracionRefresh = fechaExpiracionRefresh,
                    IdEstado = 1 // Activo
                };

                _context.InicioSesiones.Add(sesion);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Login exitoso para usuario: {Nombre}", nombre);

                await _bitacoraService.RegistrarAsync(
                    usuario: nombre,
                    accion: "LOGIN",
                    resultado: "EXITO",
                    descripcion: $"Login exitoso - Sesión ID: {sesion.IdSession}",
                    servicio: "ServicioAutenticacion.ValidarYAutenticar"
                );

                return new SesionUsuario
                {
                    IdUsuario = usuario.IdUsuario.ToString(),
                    Nombre = usuario.Usuario,
                    Token = token,
                    RefreshToken = refreshToken,
                    FechaExpiracion = fechaExpiracion
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en autenticación para usuario: {Nombre}", nombre);

                await _bitacoraService.RegistrarAsync(
                    usuario: nombre,
                    accion: "LOGIN",
                    resultado: "ERROR",
                    descripcion: $"Error interno: {ex.Message}",
                    servicio: "ServicioAutenticacion.ValidarYAutenticar"
                );

                throw;
            }
        }

        private string GenerarToken(string idUsuario, string nombre)
        {
            var secretKey = _configuration["Settings:SecretKey"]
                ?? throw new InvalidOperationException("Falta SecretKey en configuración");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, idUsuario),
                new Claim(ClaimTypes.Name, nombre),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerarRefreshToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        public async Task<SesionUsuario?> RefrescarToken(string refreshToken)
        {
            try
            {
                var sesion = await _context.InicioSesiones
                    .Include(s => s.IdUsuarioNavigation)
                    .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken &&
                                              s.FechaExpiracionRefresh > DateTime.UtcNow &&
                                              s.IdEstado == 1);

                if (sesion == null)
                {
                    return null;
                }

                // Generar nuevo token
                var nuevoToken = GenerarToken(
                    sesion.IdUsuario.ToString(),
                    sesion.IdUsuarioNavigation?.Usuario ?? "Usuario"
                );

                var nuevaExpiracion = DateTime.UtcNow.AddHours(8);

                // Actualizar sesión
                sesion.JwtToken = nuevoToken;
                sesion.FechaExpiracionToken = nuevaExpiracion;
                await _context.SaveChangesAsync();

                return new SesionUsuario
                {
                    IdUsuario = sesion.IdUsuario.ToString(),
                    Nombre = sesion.IdUsuarioNavigation?.Usuario ?? "Usuario",
                    Token = nuevoToken,
                    RefreshToken = refreshToken,
                    FechaExpiracion = nuevaExpiracion
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refrescando token");
                throw;
            }
        }

        public async Task<bool> ValidarToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var secretKey = _configuration["Settings:SecretKey"];
                var key = Encoding.UTF8.GetBytes(secretKey!);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, validationParameters, out _);

                // Verificar que exista en BD y esté activo
                var existe = await _context.InicioSesiones
                    .AnyAsync(s => s.JwtToken == token &&
                                   s.FechaExpiracionToken > DateTime.UtcNow &&
                                   s.IdEstado == 1);

                return existe;
            }
            catch
            {
                return false;
            }
        }

        public async Task CerrarSesion(int idSession)
        {
            var sesion = await _context.InicioSesiones.FindAsync(idSession);
            if (sesion != null)
            {
                sesion.IdEstado = 2; // Inactivo
                await _context.SaveChangesAsync();

                await _bitacoraService.RegistrarAsync(
                    usuario: sesion.IdUsuario.ToString(),
                    accion: "LOGOUT",
                    resultado: "EXITO",
                    descripcion: $"Sesión cerrada: {idSession}",
                    servicio: "ServicioAutenticacion.CerrarSesion"
                );
            }
        }
    }
}