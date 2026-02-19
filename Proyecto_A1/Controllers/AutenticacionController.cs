using DataAccess.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Plugins;
using Servicios;
using Servicios.DTOs;  // ← AGREGADO: Para usar los DTOs

namespace Proyecto_A1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutenticacionController : ControllerBase
    {
        private readonly ServicioAutenticacion _servicio;

        public AutenticacionController(ServicioAutenticacion servicio)
        {
            _servicio = servicio;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)  // ← Usa el DTO externo
        {
            try
            {
                // Validar datos de entrada
                if (request == null || string.IsNullOrWhiteSpace(request.Usuario) ||
                    string.IsNullOrWhiteSpace(request.Contrasena))
                {
                    return BadRequest(new
                    {
                        codigo = -1,
                        descripcion = "Usuario y contraseña son requeridos"
                    });
                }

                // Añadir una variable local para estadoRegistro asi no se pasa como referencia y se evita el error CS1612
                string estadoRegistro = string.Empty;

                // Nota: ValidarYAutenticar espera (int idUsuario, string nombre, string password)
                // Pero tu DTO tiene Usuario y Contrasena. Ajusta según sea necesario.
                var sesion = await _servicio.ValidarYAutenticar(
                    request.IdUsuario,  // ← int, no string
                    request.Usuario,    // ← nombre de usuario
                    request.Contrasena, // ← contraseña
                    ref estadoRegistro); // ← pasar por referencia si es necesario

                if (sesion == null)
                {
                    // Registrar intento fallido (opcional)
                    return Unauthorized(new
                    {
                        codigo = -1,
                        descripcion = "Usuario o contraseña incorrectos"
                    });
                }

                // Crear respuesta usando LoginResponse DTO
                var response = new LoginResponse
                {
                    ExpiresIn = sesion.FechaExpiracionToken ?? DateTime.UtcNow.AddMinutes(5),
                    AccessToken = sesion.JwtToken ?? string.Empty,
                    RefreshToken = sesion.RefreshToken ?? string.Empty,
                    UsuarioID = sesion.IdUsuario
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Log del error (asumiendo que tienes ILogger, si no, usa Console.WriteLine)
                Console.WriteLine($"Error en login: {ex.Message}");

                return StatusCode(500, new
                {
                    codigo = -1,
                    descripcion = "Error interno del servidor"
                });
            }
        }
    }

    // La clase anidada LoginRequest ha sido ELIMINADA de aquí
}