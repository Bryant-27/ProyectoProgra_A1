using System.IdentityModel.Tokens.Jwt;
using DataAccess.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Servicios
{
    //KNG - Tokens para validacion 
    public class ServicioAutenticacion
    {
        private readonly DbContext_Tokens _context;
        private readonly string _secretKey;

        public ServicioAutenticacion(DbContext_Tokens context, IConfiguration config)
        {
            _context = context;
            _secretKey = config["Settings:SecretKey"] ?? "ClaveSuperSecretaDeMasDe32Caracteres123456";
        }

        public async Task<InicioSesion?> ValidarYAutenticar(int idUsuario, string nombre, string password)
        {
            var usuario = await _context.Usuarios
                .Where(u => u.IdUsuario == idUsuario && u.Usuario == nombre && u.Contraseña == password)
                .Select(u => new Usuarios
                {
                    IdUsuario = u.IdUsuario,
                    Usuario = u.Usuario 
                })
                .FirstOrDefaultAsync();

            if (usuario == null) return null;

            int estadoRegistro = 1;
            string? tokenGenerado = null;
            DateTime fechaInicio = DateTime.UtcNow;
            DateTime fechaExpiracion = fechaInicio.AddMinutes(5);

            


            try
            {
                tokenGenerado = GenerarToken(usuario, fechaExpiracion);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generando token: " + ex.Message);
                estadoRegistro = 5;
            }

            var sesion = new InicioSesion
            {
                IdUsuario = usuario.IdUsuario,
                FechaInico = fechaInicio,
                FechaExpiracionToken = fechaExpiracion,
                JwtToken = tokenGenerado ?? "FALLO_GENERACION",
                IdEstado = estadoRegistro
            };

            _context.InicioSesiones.Add(sesion);
            await _context.SaveChangesAsync();

            return (estadoRegistro == 5) ? null : sesion;
        }

        public string GenerarToken(Usuarios usuario, DateTime expiracion)
        {
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.Usuario ?? "Usuario")
            }),
                NotBefore = DateTime.UtcNow, 
                Expires = DateTime.UtcNow.AddMinutes(5), 
                SigningCredentials = new SigningCredentials(
        new SymmetricSecurityKey(key),
        SecurityAlgorithms.HmacSha256Signature
                )
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // Método para registrar el estado 2 (Inactivo) al cerrar sesión
        public async Task CerrarSesion(int idSession)
        {
            var sesion = await _context.InicioSesiones.FindAsync(idSession);
            if (sesion != null)
            {
                sesion.IdEstado = 2; // Inactivo
                await _context.SaveChangesAsync();
            }
        }
    }
}