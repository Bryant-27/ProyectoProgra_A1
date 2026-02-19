using DataAccess.Models;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Servicios
{
    public class ServicioAutenticacion
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServicioAutenticacion> _logger;

        public ServicioAutenticacion(IConfiguration configuration, ILogger<ServicioAutenticacion> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<SesionUsuario?> ValidarYAutenticar(string idUsuario, string nombre, string password, string estadoRegistro)
        {
            // Validación básica - implementar tu lógica real aquí
            if (string.IsNullOrEmpty(idUsuario) || string.IsNullOrEmpty(password))
            {
                return null;
            }

            // Generar token JWT
            var token = GenerarToken(idUsuario, nombre);

            return new SesionUsuario
            {
                IdUsuario = idUsuario,
                Nombre = nombre,
                Token = token,
                FechaExpiracion = DateTime.Now.AddHours(8)
            };
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
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class SesionUsuario
    {
        public string IdUsuario { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime FechaExpiracion { get; set; }
    }
}