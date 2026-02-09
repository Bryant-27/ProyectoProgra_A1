using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Logica_Negocio.Services
{
    public class AuthService
    {
        private readonly PagosMovilesContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(PagosMovilesContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Validar token JWT
        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return false;

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "DefaultKey123456789012345678901234567890");

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        // Método para login (HU SRV5)
        public async Task<LoginResponse> LoginAsync(string usuario, string contraseña)
        {
            try
            {
                // Buscar usuario
                var user = await _context.Usuarios
                    .Include(u => u.IdEstadoNavigation)
                    .Include(u => u.IdRolNavigation)
                    .FirstOrDefaultAsync(u => u.Usuario == usuario);

                if (user == null || user.IdEstado != 1) // 1 = Activo
                    return new LoginResponse { Success = false, Message = "Usuario no encontrado o inactivo" };

                // En producción, usar BCrypt o similar para verificar contraseña
                // Por ahora, comparación simple (encriptar en creación)
                if (user.Contraseña != contraseña) // Debería ser hash
                    return new LoginResponse { Success = false, Message = "Contraseña incorrecta" };

                // Generar token JWT
                var token = GenerateJwtToken(user);

                return new LoginResponse
                {
                    Success = true,
                    Token = token,
                    UsuarioId = user.IdUsuario,
                    Nombre = user.NombreCompleto,
                    Rol = user.IdRolNavigation?.Nombre ?? "Usuario"
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        private string GenerateJwtToken(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "DefaultKey123456789012345678901234567890");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                    new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                    new Claim(ClaimTypes.Role, usuario.IdRol.ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(5), // 5 minutos como en la especificación
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public int? UsuarioId { get; set; }
        public string? Nombre { get; set; }
        public string? Rol { get; set; }
    }
}
